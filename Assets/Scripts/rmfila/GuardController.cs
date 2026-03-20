using System.Collections;
using UnityEngine;
using UnityEngine.AI;
// used over calling GameEvents.AlertEvent
using static GameEvents;


// used to distinguish the guards state
public enum GuardTier { 
    // default starting state, not erratic, patrolling or static
    // once a guard leaves this state, it can never go back
    Tier1 = 1,
    // erratic, but not chasing. The guard is patrolling or static
    // but also searching around for the player in their area. they
    // keep the red cone
    Tier2 = 2, 
    // guard is chasing the player and try to arrest them, nonlethal
    Tier3 = 3, 
    // guard is chasing the player and trying to shoot them, lethal
    Tier4 = 4 
}


public class GuardController : MonoBehaviour
{
    // initialize to tier 1, used by ChaseBarDisplay
    public GuardTier current_tier = GuardTier.Tier1;
    // cops heard gunshot and will be going lethal
    public bool guns_out = false;



    [Header("Patrol Settings")]
    [Tooltip("Toggle between static and patrolling guard.")]
    [SerializeField] private bool is_patrol = false;
    [Tooltip("Drag two empty game objects as patrol points.")]
    [SerializeField] private Transform point_a;
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 3f;
    [Tooltip("How long the guard pauses at each endpoint before" +
        " turning around.")]
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    [Tooltip("Speed used in chase/erratic state.")]
    [SerializeField] private float erratic_speed = 7f;
    [Tooltip("Seconds out of vision cone before bar starts decaying.")]
    [SerializeField] private float lost_sight_delay = 1.25f;

    [Header("Chase Bar")]
    [Tooltip("Full duration of the chase bar in seconds.")]
    [SerializeField] private float chase_bar_max = 8f;
    [Tooltip("How fast the bar drains when the player is out of the vision" +
        " cone and after lost_sight_delay.")]
    [SerializeField] private float chase_bar_decay = 1f;
    [Tooltip("How fast the bar refills when the player is in the vision " +
        "cone.")]
    [SerializeField] private float chase_bar_refill = 8f;

    [Header("Search Settings")]
    [Tooltip("Search speed.")]
    [SerializeField] private float erratic_turn_speed = 60f;
    [Tooltip("Safety net if guard gets stuck while returning.")]
    [SerializeField] private float return_timeout = 30f;

    [Header("Alert Settings")]
    [Tooltip("Guards within this radius will hear a gunshot.")]
    [SerializeField] private float gunshot_alert_radius = 5f;

    // set by GuardVisionCone each detection tick
    private bool can_see_player = false;

    // the current current_chase_bar in seconds
    private float current_chase_bar = 0f;

    // cached on Start so the guard can return home after searching
    private Vector3 start_position;
    private Quaternion start_rotation;

    // last confirmed player position, updated while guard has sight
    private Vector3 player_last_position;
    // direction guard was facing the moment sight
    // was lost, used for search
    private Vector3 player_last_direction;

    // the player
    private Transform target_point;
    // guard needs to pause once it gets to the destination
    private bool is_paused = false;
    // how long the guard has waited
    private float pause_time = 0f;
    private float lost_sight_timer = 0f;
    private bool spotter_player = false;

    // search / return to start
    private bool is_searching = false;
    private bool is_returning = false;
    private Vector3 current_destination;

    private Coroutine active_routine = null;
    private Coroutine tier_2_search_routine = null;

    // this is to give grace period to player if they bump into guard
    // idk, it felt wierd that when the player ran into the guard,
    // it immediately game overed with no feedback. I feel like seeing
    // the guard flip to chase mode and "attack" the player felt better
    private bool is_catching = false;

    private NavMeshAgent guard;
    private Transform player;




    // GuardVisionCone calls this every detection tick
    public void SpottedPlayer(bool value)
    {
        can_see_player = value;

        // false if player is not inside guard's spherecast
        // false if player is inside spherecast, but not inside vision cone
        // false if raycast to player hits a wall
        if (can_see_player == false)
            return;

        // update last known position while we have sight
        if (player != null)
            player_last_position = player.position;

        // if returning or searching for player and spots them
        // chase right away
        if (is_searching || is_returning)
            OverrideSearchAndReturn();

        // change to tier 3 if the guard was idle or erratic
        if (current_tier == GuardTier.Tier1 || 
            current_tier == GuardTier.Tier2)
            TierEnhancer(GuardTier.Tier3);
    }


    // used by ChaseBarDisplay
    public float GetChaseBarRatio()
    {
        if (chase_bar_max > 0f)
            return current_chase_bar / chase_bar_max;

        else
            return 0f;
    }



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

        // wait till game starts, then cache a
        // reference to the player for later use
        GameObject player_object = GameObject.FindWithTag("Player");

        if (player_object != null)
            player = player_object.transform;

        else
            Debug.LogWarning("No GameObject with the " +
                "tag 'Player' was found.");

        // if patrol is set, head towards point_a
        if (is_patrol == true && point_a != null && point_b != null)
        {
            target_point = point_a;
            // move at patrol speed
            guard.speed = patrol_speed;
            guard.SetDestination(target_point.position);
        }
    }


    // detection to see if player was spotted and to chase them
    private void FixedUpdate()
    {
        if (is_searching == true || is_returning == true)
            return;

        // if guard is tier 1 or 2 and is set to patrol, let him patrol
        // speed difference is handled by ChangeSpeed()
        if ((current_tier == GuardTier.Tier1 ||
            current_tier == GuardTier.Tier2) && is_patrol)
            Patrol();

        // we do not care about tier 3, as chaseplayer is handled elsewhere
        else if (current_tier == GuardTier.Tier4 || 
            current_tier == GuardTier.Tier3)
        {
            ChaseBarControl();

            // continue to chase the player
            ChasePlayer();
        }
    }


    private void TierEnhancer(GuardTier tier)
    {
        // can never go back a tier, that is set by DropTier() which can
        // only ever go back to tier 2
        if (tier <= current_tier)
            return;

        // only goes up
        current_tier = tier;

        // guards will KOS of the player
        if (tier == GuardTier.Tier4)
            guns_out = true;

        // set the chase bar to max as this TierEnhancer runs when starting
        // a chase
        current_chase_bar = chase_bar_max;
        spotter_player = true;
        // reset lost sight timer
        lost_sight_timer = 0f;

        ChangeSpeed();
        Debug.Log(gameObject.name + " escalated to " + tier);
    }


    private void ErraticStateChange()
    {
        // if a guard is tier 3 or 4, they can only ever drop to tier 2
        // this is because if the guard spots the player or is alerted to
        // the player's presence at all in the game, they now are on alert
        // and are more erratic in their behavior
        current_tier = GuardTier.Tier2;

        ChangeSpeed();

        // only static guards search in place
        // patrol guards get their erratic behavior from faster patrol speed
        if (is_patrol == false)
            tier_2_search_routine = StartCoroutine(Tier2SearchRoutine());

        Debug.Log(gameObject.name + " dropped to Tier 2 (permanent).");
    }


    private void ChangeSpeed()
    {
        // tier 1 is default
        if (current_tier == GuardTier.Tier1)
            guard.speed = patrol_speed;

        // anything else is erratic
        else
            guard.speed = erratic_speed;
    }


    private void ChaseBarControl()
    {
        if (can_see_player)
        {
            // reset timer the moment guard sees the player again
            lost_sight_timer = 0f;
            spotter_player = true;

            // if player is in vision cone, either constantly refill the
            // chase bar by the refill amount, or take the maximum
            // this is to say that when the player is in the vision cone
            // the current_chase_bar will not decay but increase,
            // or hit the limit
            current_chase_bar = Mathf.Min(
                current_chase_bar + chase_bar_refill * Time.fixedDeltaTime,
                chase_bar_max
            );
        }

        else
        {
            // lock the forward direction and begin search when bar is empty
            if (spotter_player == true)
            {
                player_last_direction = transform.forward;
                spotter_player = false;
            }

            // otherwise, tick up timer while player is not visible
            lost_sight_timer += Time.fixedDeltaTime;

            // keep updating the last unkown position
            // while the timer is still counting
            if (lost_sight_timer < lost_sight_delay && player != null)
                player_last_position = player.position;

            // timer triggered for lost sight, get the player's last position
            // at the exact time the guard is going to start searching
            if (lost_sight_timer >= lost_sight_delay &&
                    lost_sight_timer - Time.fixedDeltaTime < lost_sight_delay &&
                    player != null)
            {
                player_last_position = player.position;
            }
        }
    }


    private void ChasePlayer()
    {
        Vector3 destination;

        if (can_see_player == true || 
            lost_sight_timer < lost_sight_delay)
            // chases player when in sight
            destination = player.position;

        else
            // last known position when not
            destination = player_last_position;

        // only set a new destination if it actually changed
        // resetting every frame resets the agent's path state
        // and prevents the search condition from triggering
        if (Vector3.Distance(destination, current_destination) > 0.2f)
        {
            guard.SetDestination(destination);
            current_destination = destination;
        }

        if (guard.velocity.sqrMagnitude > 0.01f)
            RotateGuard(10f);

        if (!can_see_player &&
            (lost_sight_timer >= lost_sight_delay) &&
            (!guard.pathPending &&
            guard.remainingDistance <= guard.stoppingDistance + 0.3f) &&
            active_routine == null)
        {
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    private void Patrol()
    {
        if (target_point == null)
            return;

        // if paused at endpoint, countdown to move again
        if (is_paused == true)
        {
            // decrement pause time
            pause_time -= Time.fixedDeltaTime;

            // pause time expired, unpause and move
            if (pause_time <= 0f)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }

            // otherwise do nothing/remain still/paused
            return;
        }

        RotateGuard(10f);

        // check if the guard has reached the target point by
        // checking to see agent  is  still computing  a path
        if (guard.pathPending == false && guard.remainingDistance <=
            guard.stoppingDistance)
        {
            // pause at the endpoint as the guard has reached the target
            is_paused = true;
            // set the timer countdown
            pause_time = pause_duration;
            guard.ResetPath();

            // update the target_point to be the opposite point
            if (target_point == point_a)
                target_point = point_b;

            else
                target_point = point_a;
        }
    }


    private void RotateGuard(float rotate_speed)
    {
        // we only rotate if the movement is substantial enough
        if (guard.velocity.sqrMagnitude <= 0.01f)
            return;

        // the current direction the guard is going
        Vector3 direction = guard.velocity.normalized;
        // rotate the guard to face the player around the y-axis
        // achieved via Atan2, takes distances we found to the player
        // and converts those X and Y vals to be the rotation pointing
        // in that direction
        float angle =
            Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        // rotate the guard, this will point the vision cone too
        // since we are casting out as forward
        Quaternion target_rotation = Quaternion.Euler(0f, angle, 0f);
        // lerp instead of instantly setting the direction
        // this smoothly turns the guards
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            target_rotation,
            rotate_speed * Time.fixedDeltaTime
        );
    }


    // ends the search/return coroutine so
    // the guard can immediately chase again
    private void OverrideSearchAndReturn()
    {
        if (active_routine != null)
        {
            StopCoroutine(active_routine);
            active_routine = null;
        }

        // also kill the tier 2 search if it was running
        if (tier_2_search_routine != null)
        {
            StopCoroutine(tier_2_search_routine);
            tier_2_search_routine = null;
        }

        is_searching = false;
        is_returning = false;
    }



    private void OnAlertEvent(AlertEvent e)
    {
        // set last known to player's position so the guard investigates
        // the room where the alert came from, not Vector3.zero
        if (player != null)
            player_last_position = player.position;

        // laser / collectible alert escalate to at least Tier 3
        if (current_tier < GuardTier.Tier3)
            TierEnhancer(GuardTier.Tier3);

        Debug.Log(gameObject.name + " received AlertEvent.");
    }


    private void OnGunshotEvent(GunshotEvent e)
    {
        // distance between where gunshot was soundedf and guard
        float distance = 
            Vector3.Distance(transform.position, e.player_position);

        // check the distance, return if outside range
        if (distance > gunshot_alert_radius)
            return;

        // move to where the player was when they fired, not their current spot
        player_last_position = e.player_position;
        // guard will shoot because it heard player shoot
        guns_out = true;

        // if they are already doing something, this event overrides it
        if (is_searching || is_returning)
            OverrideSearchAndReturn();

        // enhance to max tier, lethal tier
        TierEnhancer(GuardTier.Tier4);

        Debug.Log(gameObject.name + " heard gunshot, escalating to Tier 4.");
    }


    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Player") == false)
            return;

        // already chasing = instant game over, no grace
        if (current_tier == GuardTier.Tier3 || current_tier == GuardTier.Tier4)
        {
            Debug.Log("The player has been caught, Game Over!");
            EventBus.Publish(new GameOverEvent());
            return;
        }

        // first contact = start chasing and give 1 second to flee
        TierEnhancer(GuardTier.Tier3);

        if (is_catching == false)
            StartCoroutine(GracePeriod());
    }


    // only can be called 1 time and thats on the initial contact with guard
    // after this, if the player doesn't move from the guard collision field
    // by the grace timer's end, it will be called again and have is_catching
    // = to false, so it will game over. Otherwise, if the player leaves and
    // enters again, that will call OnTriggerEnter, and this cannot be called
    // again.
    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        // Still touching after grace period expired = game over
        if (collision.CompareTag("Player") && 
            !is_catching &&
            (current_tier == GuardTier.Tier3 || 
            current_tier == GuardTier.Tier4))
        {
            Debug.Log("Player caught! Game Over.");
            EventBus.Publish(new GameOverEvent());
        }
    }


    private IEnumerator SearchAndReturnRoutine()
    {
        is_searching = true;

        guard.ResetPath();

        if (current_chase_bar > 0f)
        {
            // anchor the searching turn angle to be around the direction
            // we were last facing when sight was lost
            float anchor_angle =
                Mathf.Atan2(player_last_direction.x,
                player_last_direction.z) * Mathf.Rad2Deg;

            // guard looks to left, right,
            // then back to center, then repeats
            // search_turn_angle directly controls
            // how far left and right it looks
            float current_offset = 0f;
            // full 360 left, all the way right, back to center
            float[] turn_targets = { -180f, 180f, 0f };
            int target_index = 0;

            while (current_chase_bar > 0f)
            {
                current_chase_bar = 
                    Mathf.Max(
                        current_chase_bar - chase_bar_decay * 
                        Time.deltaTime, 0f);

                float turn_step = erratic_turn_speed * Time.deltaTime;     
                
                current_offset = 
                    Mathf.MoveTowards(current_offset, 
                    turn_targets[target_index], 
                    turn_step);

                // once we hit the target, advance to the next one
                if (Mathf.Approximately(
                    current_offset, 
                    turn_targets[target_index])
                )
                    target_index = (target_index + 1) % turn_targets.Length;

                transform.rotation = 
                    Quaternion.Euler(0f, anchor_angle + current_offset, 0f);
                yield return null;
            }
        }

        // searching is done, permanently drop to erratic
        is_searching = false;
        is_returning = true;

        ErraticStateChange();
        // return to start
        guard.SetDestination(start_position);

        // wait for guard to arrive and timeout so we never hang
        float return_time_taken = 0f;

        // this should always end by the guard reaching its return point
        // but just incase, return_timeout is a safety net to kill it if
        // its taking too long
        while (return_time_taken < return_timeout &&
               (guard.pathPending == true || 
               guard.remainingDistance > guard.stoppingDistance + 0.1f))
        {
            return_time_taken += Time.deltaTime;
            RotateGuard(10f);
            yield return null;
        }

        if (is_patrol && point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.SetDestination(target_point.position);
        }

        is_returning = false;
        active_routine = null;
    }


    private IEnumerator Tier2SearchRoutine()
    {
        while (true)
        {
            Quaternion look_right = start_rotation * Quaternion.Euler(0f, 180f, 0f);

            while (Quaternion.Angle(transform.rotation, look_right) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, look_right, erratic_turn_speed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(pause_duration);

            while (Quaternion.Angle(transform.rotation, start_rotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, start_rotation, erratic_turn_speed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(pause_duration);
        }
    }


    // gives player that touched guard a moment of time
    private IEnumerator GracePeriod()
    {
        // now can no longer get grace period
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        // set to false that way on trigger enter can call a game over
        // either way, this cannot be called again as explained above
        is_catching = false;
    }
}


// credits
// https://docs.unity3d.com/2022.2/Documentation/Manual/Rigidbody2D-Kinematic.html
//
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html