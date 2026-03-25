using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;


public enum GuardTier {
    // default calm state. once left, never returns here
    Tier1 = 1,
    // permanently alert, erratic patrol/scan. lowest possible after any chase
    Tier2 = 2,
    // actively chasing, nonlethal
    Tier3 = 3,
    // actively chasing, lethal (heard gunshot)
    Tier4 = 4 
}


public enum GuardMode
{
    // stands in place, does not move in tier 1
    Static,
    // walks between point_a and point_b
    Patrol,
    // stands in place but scans back and forth from the start
    StaticSearch
}


public class GuardController : MonoBehaviour
{
    // initialize to tier 1, used by ChaseBarDisplay
    public GuardTier current_tier = GuardTier.Tier1;
    // when guards hear player shoot, go lethal
    public bool guns_out = false;

    [Header("Guard Mode")]
    [Tooltip("Static = stands still. Patrol = walks between " +
    "point_a and point_b. StaticSearch = stands still but " +
    "scans back and forth.")]
    [SerializeField] private GuardMode guard_mode = GuardMode.Static;

    [Header("Patrol Settings")]
    [SerializeField] private Transform point_a;
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 1f;
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    [SerializeField] private float erratic_speed = 3f;

    [Header("Chase Bar")]
    [SerializeField] private float chase_bar_max = 4f;
    [SerializeField] private float chase_bar_decay = 1f;
    [SerializeField] private float chase_bar_refill = 4f;

    [Header("Search Settings")]
    [SerializeField] private float turn_speed = 100f;
    [Tooltip("How far past the last seen position the guard " +
    "runs in the player's travel direction before sweeping.")]
    [SerializeField] private float overshoot_distance = 3f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bullet_prefab;
    [SerializeField] private Transform fire_point;
    [SerializeField] private float fire_rate = 1f;
    [SerializeField] private float shoot_range = 10f;
    [SerializeField] private float gunshot_alert_radius = 10f;
    [Tooltip("How long the guard waits until it shoots upon catching " +
        "the player.")]
    [SerializeField] private float sight_delay = 0.5f;


    // set by GuardVisionCone each detection tick
    private bool can_see_player = false;
    private float current_chase_bar = 0f;

    // cached on Start for returning home after a search
    private Vector3 start_position;
    private Quaternion start_rotation;

    // frozen at sight loss, only overwritten by
    // UpdateChaseBar
    // OnGunshotEvent
    // OnAlertEvent
    private Vector3 player_last_position;

    // player's travel direction at the moment sight was lost
    // used as the overshoot heading and search sweep anchor
    private Vector3 player_last_direction;
    // previous tick's player position, used to compute travel direction
    private Vector3 player_previous_position;

    private Transform target_point;
    private Transform player;
    private NavMeshAgent guard;
    private GameObject guard_weapon;

    // patrol pause
    private bool is_paused = false;
    private float pause_time = 0f;

    private float return_timeout = 30f;

    // chase tracking
    private bool had_sight = false;
    private Vector3 current_destination;

    // when true, guard walks to player_last_position with no chase
    // bar logic. always arrives. set by alert and gunshot events.
    // cleared when guard spots the player or arrives and starts searching
    private bool is_investigating = false;

    // search / return to start
    private bool is_searching = false;
    private bool is_returning = false;

    private Coroutine active_routine = null;
    private Coroutine static_search_routine = null;

    // grace period so bumping a t1 guard isn't instant game over
    // seeing the guard flip to chase mode first feels better
    private bool is_catching = false;

    // shoot timer
    private float next_fire_time = 0f;
    private float sight_timer = 0f;


    // ----- START PUBLIC ----- \\
    public void SpottedPlayer(bool value)
    {
        can_see_player = value;

        if (can_see_player == false)
            return;

        // guard spotted player while investigating an alert/gunshot
        // switch to normal vision-based chase
        if (is_investigating)
            is_investigating = false;

        CancelSearchAndReturn();

        // stops regressing back to tier 3 after pulling guns out
        if (guns_out)
            TierUp(GuardTier.Tier4);
        else if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);

    }


    public float GetChaseBarRatio()
    {
        // if chase bar is not decayed, display remaining ratio
        if (chase_bar_max > 0f)
            return current_chase_bar / chase_bar_max;
        else
            return 0f;
    }
    // ----- END PUBLIC ----- \\


    // ----- START MAIN LIFE ----- \\
    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GunshotEvent>(OnGunshotEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<GunshotEvent>(OnGunshotEvent);
    }


    private void Awake()
    {
        guard = GetComponentInChildren<NavMeshAgent>();

        // keep guard flat for top-down
        guard.updateRotation = false;
        guard.updateUpAxis = false;
    }


    private void Start()
    {
        start_position = transform.position;
        start_rotation = transform.rotation;

        guard_weapon = fire_point.parent.gameObject;
        guard_weapon.SetActive(false);

        GameObject player_object = GameObject.FindWithTag("Player");

        if (player_object != null)
        {
            player = player_object.transform;
            // update the var so it won't be at origin
            player_previous_position = player.position;
        }

        else
            Debug.LogWarning("No GameObject with tag 'Player' found.");

        if (guard_mode == GuardMode.Patrol && 
            point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.speed = patrol_speed;
            guard.SetDestination(target_point.position);
        }

        // static search guards scan from the start in tier 1
        if (guard_mode == GuardMode.StaticSearch)
            static_search_routine = StartCoroutine(StaticScanRoutine());
    }


    private void FixedUpdate()
    {
        if (is_searching == true || is_returning == true)
            return;

        // tier 1 & 2 - patrol or idle (speed diff handled by ApplySpeed)
        if (current_tier <= GuardTier.Tier2)
        {
            if (guard_mode == GuardMode.Patrol)
                Patrol();

            // static and static_search do nothing here
            // static_search is handled by its coroutine
            return;
        }

        // tier 3 & 4 - actively chasing
        if (current_tier >= GuardTier.Tier3)
        {
            // investigating = walking to alert/gunshot location
            // no chase bar logic, guard always arrives
            if (is_investigating)
            {
                Investigate();
                ShootAtPlayer();
                return;
            }

            UpdateChaseBar();
            ChasePlayer();
            ShootAtPlayer();
        }
    }
    // ----- END MAIN LIFE ----- \\


    // ----- START TIER ----- \\
    // can only move up, DropErratic is the way to downgrade
    private void TierUp(GuardTier tier)
    {
        if (tier <= current_tier)
            return;

        current_tier = tier;

        if (tier == GuardTier.Tier4)
            guns_out = true;

        // fill bar and reset sight tracking for the new chase
        current_chase_bar = chase_bar_max;
        had_sight = true;

        ApplySpeed();
        Debug.Log(gameObject.name + " escalated to " + tier);
    }


    // called after the search timer expires guard becomes
    // permanently alert but stops actively chasing
    private void ErraticDrop()
    {
        current_tier = GuardTier.Tier2;
        ApplySpeed();

        // static guards scan in place
        // patrol guards get erratic behavior from faster patrol speed
        if (guard_mode != GuardMode.Patrol)
            static_search_routine = StartCoroutine(StaticScanRoutine());

        Debug.Log(gameObject.name + " dropped to Tier 2 (permanent).");
    }


    private void ApplySpeed()
    {
        // tier 1 is default anything else is erratic
        if (current_tier == GuardTier.Tier1)
            guard.speed = patrol_speed;
        else
            guard.speed = erratic_speed;
    }
    // ----- END TIER ----- \\


    // ----- START CHASE ----- \\
    // tracks position + direction while guard has sight, freezes on loss
    // player_last_position only updates here if the guard can see the player
    private void UpdateChaseBar()
    {
        if (can_see_player == true)
        {
            had_sight = true;

            // update last known position while visible
            if (player != null)
            {
                player_last_position = player.position;

                // compute player's travel direction
                // if the player is standing still, keep the previous
                // direction so the overshoot isnt zeroed out
                Vector3 difference =
                    player.position - player_previous_position;

                if (difference.sqrMagnitude > 0.01f)
                    player_last_direction = difference.normalized;

                player_previous_position = player.position;
            }

            // refill bar while visible, capped at max
            current_chase_bar = Mathf.Min(
                current_chase_bar + chase_bar_refill * Time.fixedDeltaTime,
                chase_bar_max);
            return;
        }

        // just lost sight
        // direction already frozen from last
        // visible tick, just flip the flag
        if (had_sight == true)
            had_sight = false;
    }


    // moves guard towards last known position (freezes at sight loss)
    private void ChasePlayer()
    {
        Vector3 destination;

        if (can_see_player == true)
            // chases player when in sight
            destination = player.position;
        else
            // last known position when not
            // this was set by previous functions to get the
            // players last location after the loss sight delay
            // was over. Before it was using its last KNOWN position
            destination = player_last_position;

        // only set a new destination if it actually changed
        // resetting every frame resets the agent's path state
        // and prevents the search condition from triggering
        if (Vector3.Distance(destination, current_destination) > 0.2f)
        {
            guard.SetDestination(destination);
            current_destination = destination;
        }

        // face the player directly while visible so the vision
        // cone tracks them
        if (can_see_player == true && player != null)
            FaceTarget(player.position);
        // face movement direction when chasing blind
        // so the cone points where the guard is running
        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();
        else
            FaceTarget(player_last_position);

        // reached last known position with no sight, begin search
        if (can_see_player == false && HasReachedDestination() == true &&
            active_routine == null)
        {
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    // walks to player_last_position regardless of chase bar logic
    private void Investigate()
    {
        // set destination if it changed
        if (Vector3.Distance(
            player_last_position, current_destination) > 0.2f)
        {
            guard.SetDestination(player_last_position);
            current_destination = player_last_position;
        }

        // face player if visible, face movement direction if moving,
        // otherwise face the investigation destination as a fallback
        // for when velocity is still near zero during acceleration
        if (can_see_player == true && player != null)
            FaceTarget(player.position);
        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();
        else
            FaceTarget(player_last_position);

        // arrived at investigation point, start normal search
        if (HasReachedDestination() == true && active_routine == null)
        {
            is_investigating = false;
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }
    // ----- END CHASE ----- \\


    // ----- START PATROL  ----- \\
    private void Patrol()
    {
        if (target_point == null)
            return;

        // if paused at endpoint, countdown to move again
        if (is_paused == true)
        {
            // rotate to face the next point while paused
            FaceTarget(target_point.position);

            // decrement pause time
            pause_time -= Time.fixedDeltaTime;

            // check if the guard has finished turning to the next point
            Vector3 to_target =
                (target_point.position - transform.position).normalized;
            // true if the angle between the current rotation 
            // and target is less than 1
            bool facing_target =
                Quaternion.Angle(transform.rotation, 
                YawFromDirection(to_target)) < 1f;

            // only unpause once the timer is done AND the turn is complete
            // this means the guard will always fully rotate before walking
            if (pause_time <= 0f && facing_target == true)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }

            // otherwise do nothing/remain still/paused
            return;
        }

        // match the guard's facing direction to how it is moving
        FaceMovementDirection();

        // check if the guard has reached the target point by
        // checking to see agent  is  still computing  a path
        if (HasReachedDestination() == true)
        {
            is_paused = true;
            pause_time = pause_duration;
            guard.ResetPath();

            // update the target_point to be the opposite point
            if (target_point == point_a)
                target_point = point_b;
            else
                target_point = point_a;
        }
    }
    // ----- END PATROL ----- \\


    // ----- START SHOOT LOGIC ----- \\
    private void ShootAtPlayer()
    {
        if (current_tier < GuardTier.Tier4 || can_see_player == false || player == null)
            return;

        if (can_see_player == true)
            sight_timer += Time.fixedDeltaTime;
        else
            // reset if sight is lost
            sight_timer = 0f;

        // dont shoot unnless you can see player, and its been longer
        // than half a second so you don't die right away 
        // MAYBE REMOVE DEPENDING ON HOW PUNISHING WE WANT TO BE
        if (can_see_player == false || sight_timer < sight_delay)
            return;

        float distance = 
            Vector3.Distance(transform.position, player.position);

        if (distance > shoot_range)
            return;

        if (Time.time < next_fire_time)
            return;

        next_fire_time = Time.time + fire_rate;

        // the straight path from the bullet spawn to the player
        Vector3 direction = 
            (player.position - fire_point.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        Instantiate(bullet_prefab, fire_point.position, rotation);
    }
    // ----- END SHOOT LOGIC ----- \\


    // ----- START ROTATION HELPERS ----- \\
    // smoothly rotates the guard to face its movement direction
    // from now on, anytime the guard needs to face the directing
    // it is walking, it will call this function
    private void FaceMovementDirection()
    {
        if (guard.velocity.sqrMagnitude <= 0.01f)
            return;

        // this will get the agent's normalized current velocity, then
        // convert that to a y axis rotation, then apply that one frame
        // worth of RotateTowards to the target rotation
        SmoothRotateTo(YawFromDirection(guard.velocity.normalized));
    }


    // smoothly rotates the guard toward a world position
    private void FaceTarget(Vector3 world_position)
    {
        Vector3 direction =
            (world_position - transform.position).normalized;

        SmoothRotateTo(YawFromDirection(direction));
    }


    // applies one rotation step toward the target yaw
    private void SmoothRotateTo(Quaternion target)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            turn_speed * Time.fixedDeltaTime);
    }


    // converts an x & z direction vector into a y axis rotation
    private Quaternion YawFromDirection(Vector3 direction)
    {
        float angle =
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        return Quaternion.Euler(0f, angle, 0f);
    }
    // ----- END ROTATION HELPERS ----- \\


    // ----- START SEARCH ----- \\
    private void CancelSearchAndReturn()
    {
        if (active_routine != null)
        {
            StopCoroutine(active_routine);
            active_routine = null;
        }

        if (static_search_routine != null)
        {
            StopCoroutine(static_search_routine);
            static_search_routine = null;
        }

        is_searching = false;
        is_returning = false;
    }


    // returns true if the guard has no new paths in queue and is within
    // a tolerance of the remaining distance to the end of the path
    private bool HasReachedDestination()
    {
        return guard.pathPending == false &&
               guard.remainingDistance <= guard.stoppingDistance + 0.1f;
    }
    // ----- END SEARCH ----- \\


    // ----- START EVENT HANDLERS ----- \\
    // overrides player_last_position if called
    private void OnAlertEvent(AlertEvent e)
    {
        if (player != null)
            player_last_position = player.position;

        is_investigating = true;

        CancelSearchAndReturn();

        if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);

        Debug.Log(gameObject.name + " received AlertEvent.");
    }


    // overrides player_last_position if called
    private void OnGunshotEvent(GunshotEvent e)
    {
        // distance between the gunshot and guard
        float distance =
            Vector3.Distance(transform.position, e.player_position);

        if (distance > gunshot_alert_radius)
            return;

        // move to where the player was when they
        // fired, not their current spot
        player_last_position = e.player_position;
        guns_out = true;

        if (guard_weapon != null)
            guard_weapon.SetActive(true);

        is_investigating = true;

        CancelSearchAndReturn();

        TierUp(GuardTier.Tier4);

        Debug.Log(gameObject.name + " heard gunshot, escalating to Tier 4.");
    }
    // ----- END EVENT HANDLERS ----- \\


    // ----- START COLLISION HANDLERS ----- \\
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("PlayerBullet"))
        {
            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Player") == false)
            return;

        // already chasing = instant game over
        if (current_tier >= GuardTier.Tier3)
        {
            EndGame();
            return;
        }

        // first contact: start chase with a brief grace window
        TierUp(GuardTier.Tier3);

        if (is_catching == false)
            StartCoroutine(GracePeriodRoutine());
    }


    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.layer ==
            LayerMask.NameToLayer("PlayerBullet"))
        {
            Destroy(gameObject);
            return;
        }

        // Still touching after grace period expired = game over
        if (collision.CompareTag("Player") && is_catching == false &&
            current_tier >= GuardTier.Tier3)
        {
            EndGame();
        }
    }


    private void EndGame()
    {
        Debug.Log("Player caught! Game Over.");
        EventBus.Publish(new GameOverEvent());
    }
    // ----- END COLLISION HANDLERS ----- \\


    // ----- START COROUTINES ----- \\
    // ChasePlayer already brought us to player_last_position
    // run past in the direction the player was traveling
    // scan left/right anchored to the stopping position
    // return home
    private IEnumerator SearchAndReturnRoutine()
    {
        is_searching = true;
        guard.ResetPath();

        // run past the last seen point in the direction the player
        // was traveling. clamp to navmesh so the guard doesnt path
        // into a wall
        if (player_last_direction.sqrMagnitude > 0.01f)
        {
            Vector3 predicted_point = player_last_position +
                player_last_direction * overshoot_distance;
            
            // if the predicted wall is not within boundaries of map or 
            // within a wall, SamplePosition will find the next closest
            // spot within overshoot_distance. Otherwise, skip entirely
            if (NavMesh.SamplePosition(predicted_point, out NavMeshHit hit, 
                overshoot_distance, NavMesh.AllAreas))
            {
                guard.SetDestination(hit.position);

                // match the guard's facing direction to how it is moving
                while (HasReachedDestination() == false)
                {
                    // match the guard's facing direction to how it is moving
                    FaceMovementDirection();
                    yield return null;
                }
            }
        }

        guard.ResetPath();

        // scan left and right anchored to player_last_direction
        // the direction the player was running, not guard facing
        if (current_chase_bar > 0f)
        {
            // anchor the searching turn angle to be around the direction
            float anchor_angle = Mathf.Atan2(player_last_direction.x,
                player_last_direction.z) * Mathf.Rad2Deg;

            float offset = 0f;
            float[] sweep = { -180f, 180f, 0f };
            int index = 0;

            // while until the chase bar has decayed
            while (current_chase_bar > 0f)
            {
                current_chase_bar = 
                    Mathf.Max(current_chase_bar - chase_bar_decay * 
                        Time.deltaTime, 0f);
                
                offset = Mathf.MoveTowards(offset, sweep[index],
                    turn_speed * Time.deltaTime);

                // compare the offset we are at and the target sweep
                // angle we want. If they are close enough, move onto
                // the next sweep index. Modulo by the sweep length
                // to continuously sweep between the 3 while chase bar
                // is decaying, stops once fully decayed
                if (Mathf.Approximately(offset, sweep[index]))
                    index = (index + 1) % sweep.Length;

                transform.rotation = 
                    Quaternion.Euler(0f, anchor_angle + offset, 0f);

                yield return null;
            }
        }

        // RETURN, searching is done, permanently drop to erratic
        is_searching = false;
        is_returning = true;

        ErraticDrop();
        guard.SetDestination(start_position);

        // wait for guard to arrive and timeout so we never hang
        float time_elapsed = 0f;
        // ends once the time elapsed is done, or we reached the destination
        while (time_elapsed < return_timeout &&
            HasReachedDestination() == false)
        {
            time_elapsed += Time.deltaTime;
            FaceMovementDirection();
            yield return null;
        }

        // resume patrol if set up
        if (guard_mode == GuardMode.Patrol 
            && point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.SetDestination(target_point.position);
        }

        is_returning = false;
        active_routine = null;
    }


    // static guards scan back and forth after dropping to tier 2
    private IEnumerator StaticScanRoutine()
    {
        while (true)
        {
            Quaternion look_right =
                start_rotation * Quaternion.Euler(0f, 180f, 0f);

            yield return RotateUntilFacing(look_right);
            yield return new WaitForSeconds(pause_duration);

            yield return RotateUntilFacing(start_rotation);
            yield return new WaitForSeconds(pause_duration);
        }
    }


    // smoothly rotates until within 1 degree of target
    private IEnumerator RotateUntilFacing(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                turn_speed * 1.5f * Time.deltaTime);

            yield return null;
        }
    }


    // gives player that touched guard a moment of time
    private IEnumerator GracePeriodRoutine()
    {
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        is_catching = false;
    }
    // ----- END COROUTINES ----- \\
}


// credits
// https://docs.unity3d.com/2022.2/Documentation/Manual/Rigidbody2D-Kinematic.html
//
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html