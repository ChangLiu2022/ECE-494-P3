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
    Static,
    Patrol,
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

    [Header("Health")]
    [SerializeField] private int max_health = 2;
    [SerializeField] private float stagger_duration = 1.2f;
    [SerializeField] private float knockback_distance = 1.5f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform point_a;
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 1f;
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    [SerializeField] private float erratic_speed = 3f;

    [Header("Chase Bar")]
    [Tooltip("Time in seconds the bar takes to fill at max range.")]
    [SerializeField] private float chase_bar_max = 4f;
    [Tooltip("How fast the bar drains when player leaves sight.")]
    [SerializeField] private float chase_bar_decay = 3f;
    [Tooltip("Base fill rate per second when player is at the edge of " +
        "the vision cone. Exponentially scaled by proximity.")]
    [SerializeField] private float chase_bar_fill_rate = 1f;
    [Tooltip("How aggressively proximity speeds up the fill. " +
        "Higher = more punishing at close range.")]
    [SerializeField] private float proximity_exponent = 2f;

    [Header("Search Settings")]
    [SerializeField] private float turn_speed = 100f;
    [SerializeField] private float overshoot_distance = 3f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bullet_prefab;
    [SerializeField] private Transform fire_point;
    [SerializeField] private float fire_rate = 1f;
    [SerializeField] private float shoot_range = 10f;
    [SerializeField] private float gunshot_alert_radius = 10f;
    [Tooltip("Delay before the guard can fire after first spotting " +
        "the player in a chase. Gives the player a window to react.")]
    [SerializeField] private float sight_delay = 0.5f;
    [Tooltip("Time it takes for the guard to draw their weapon " +
        "after escalating to tier 4.")]
    [SerializeField] private float weapon_draw_time = 1f;

    [Header("Sprite Settings")]
    [SerializeField] SpriteRenderer guards_sprite_renderer;
    [SerializeField] Sprite nonlethal_guard_sprite;
    [SerializeField] Sprite lethal_guard_sprite;


    private int current_health;
    private bool is_staggered = false;

    // set by GuardVisionCone each detection tick
    private bool can_see_player = false;
    // distance to player when visible, used for fill rate scaling
    private float sight_distance = 0f;
    // the detect radius from the vision cone, used to normalize distance
    private float max_detect_radius;

    private float current_chase_bar = 0f;

    // true when the guard sees the player but the bar isn't full yet
    // guard freezes in place and stares during this phase
    private bool is_spotting = false;
    // rotation the guard was facing when it first spotted the player
    // locked during the spotting phase so the cone doesn't track
    private Quaternion spotting_rotation;

    private Vector3 start_position;
    private Quaternion start_rotation;

    private Vector3 player_last_position;
    private Vector3 player_last_direction;
    private Vector3 player_previous_position;

    private Transform target_point;
    private Transform player;
    private NavMeshAgent guard;
    private GameObject guard_weapon;
    private VisionConeMesh vision_cone_mesh;

    // patrol pause
    private bool is_paused = false;
    private float pause_time = 0f;

    private float return_timeout = 30f;

    private bool had_sight = false;
    private Vector3 current_destination;

    // when true, guard walks to player_last_position with no chase
    // bar logic. set by alert and gunshot events
    private bool is_investigating = false;

    private bool is_searching = false;
    private bool is_returning = false;

    private Coroutine active_routine = null;
    private Coroutine static_search_routine = null;

    // grace period so bumping a t1 guard isn't instant game over
    private bool is_catching = false;

    private float next_fire_time = 0f;
    private float sight_timer = 0f;
    private bool is_drawing_weapon = false;


    // ----- START PUBLIC ----- \\
    // called by GuardVisionCone every detection tick
    // distance is how far the player is from the guard (0 when not visible)
    public void SpottedPlayer(bool value, float distance)
    {
        can_see_player = value;
        sight_distance = distance;

        if (can_see_player == false)
            return;

        // guard spotted player while investigating an alert/gunshot
        // switch to normal vision-based chase
        if (is_investigating)
            is_investigating = false;

        CancelSearchAndReturn();

        // if already chasing (bar was full), keep the tier and keep
        // updating last known position. no need to re-enter spotting
        if (current_tier >= GuardTier.Tier3)
        {
            // make sure we stay tier 4 if guns are out
            if (guns_out)
                TierUp(GuardTier.Tier4);
            return;
        }

        // not chasing yet - enter the spotting/freeze phase
        // the bar will fill in FixedUpdate
        if (is_spotting == false)
        {
            is_spotting = true;
            // lock the guard's current facing direction
            spotting_rotation = transform.rotation;
            guard.ResetPath();
        }
    }


    public float GetChaseBarRatio()
    {
        if (chase_bar_max > 0f)
            return current_chase_bar / chase_bar_max;
        return 0f;
    }
    // ----- END PUBLIC ----- \\


    // ----- START MAIN LIFE ----- \\
    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GunshotEvent>(OnGunshotEvent);
        EventBus.Subscribe<ErraticAlertEvent>(OnErraticAlertEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<GunshotEvent>(OnGunshotEvent);
        EventBus.Unsubscribe<ErraticAlertEvent>(OnErraticAlertEvent);
    }


    private void Awake()
    {
        guard = GetComponentInChildren<NavMeshAgent>();
        guard.updateRotation = false;
        guard.updateUpAxis = false;
    }


    private void Start()
    {
        current_health = max_health;
        start_position = transform.position;
        start_rotation = transform.rotation;

        // firepoint is the "muzzle" of the gun
        guard_weapon = fire_point.parent.gameObject;
        // do not have the gun out by default
        guard_weapon.SetActive(false);
        guards_sprite_renderer.sprite = nonlethal_guard_sprite;

        // cache the detect radius for normalizing distance in fill calc
        vision_cone_mesh = GetComponentInChildren<VisionConeMesh>();
        if (vision_cone_mesh != null)
            max_detect_radius = vision_cone_mesh.GetDetectRadius();

        // this was rough because player's body is a child
        // to the player root, but the child has the sphere
        // collider
        GameObject player_object = GameObject.FindWithTag("Body");

        if (player_object != null)
        {
            player = player_object.transform;
            // update so it won't be at origin
            player_previous_position = player.position;
        }

        else
            Debug.LogWarning("No GameObject with tag 'Body' found.");

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
        // stagger locks out everything
        if (is_staggered)
            return;

        if (is_searching || is_returning)
            return;

        // ----- SPOTTING PHASE -----
        // guard sees player but bar isn't full yet
        // freeze in place, fill the bar
        if (is_spotting)
        {
            HandleSpotting();
            return;
        }

        // ----- TIER 1 & 2 - PATROL OR IDLE -----
        if (current_tier <= GuardTier.Tier2)
        {
            if (guard_mode == GuardMode.Patrol)
                Patrol();
            return;
        }

        // ----- TIER 3 & 4 - ACTIVELY CHASING -----
        if (current_tier >= GuardTier.Tier3)
        {
            if (is_investigating)
            {
                Investigate();
                ShootAtPlayer();
                return;
            }

            UpdateChaseTracking();
            ChasePlayer();
            ShootAtPlayer();
        }
    }
    // ----- END MAIN LIFE ----- \\


    // ----- START SPOTTING ----- \\
    // handles the freeze-and-fill phase before a chase begins
    private void HandleSpotting()
    {
        // lock facing direction to where the guard was looking
        // when it first noticed the player. the cone stays fixed
        // so the player can walk out of it
        transform.rotation = spotting_rotation;
        guard.ResetPath();

        if (can_see_player && player != null)
        {
            // fill rate scales exponentially with proximity
            // at max range normalized = 0 -> multiplier = 1
            // at point blank normalized = 1 -> multiplier = 1 + scale
            float normalized_proximity = 1f - Mathf.Clamp01(
                sight_distance / max_detect_radius);

            float fill_multiplier = 1f + proximity_exponent *
                (normalized_proximity * normalized_proximity);

            current_chase_bar = Mathf.Min(
                current_chase_bar +
                    chase_bar_fill_rate * fill_multiplier *
                    Time.fixedDeltaTime,
                chase_bar_max);

            // bar is full, begin the chase
            if (current_chase_bar >= chase_bar_max)
            {
                is_spotting = false;

                if (guns_out)
                    TierUp(GuardTier.Tier4);
                else
                    TierUp(GuardTier.Tier3);
            }
        }
        else
        {
            // player left the cone, decay the bar back to zero
            current_chase_bar = Mathf.Max(
                current_chase_bar - chase_bar_decay * Time.fixedDeltaTime,
                0f);

            // bar fully decayed, resume normal behavior
            if (current_chase_bar <= 0f)
                is_spotting = false;
        }
    }
    // ----- END SPOTTING ----- \\


    // ----- START TIER ----- \\
    // can only move up, DropErratic is the way to downgrade
    private void TierUp(GuardTier tier)
    {
        if (tier <= current_tier)
            return;

        current_tier = tier;

        // fill the bar fully so the chase timer starts from max
        current_chase_bar = chase_bar_max;
        had_sight = true;

        if (tier == GuardTier.Tier4)
        {
            guns_out = true;
            // weapon draw delay so the player has a moment to react
            if (is_drawing_weapon == false)
                StartCoroutine(DrawWeaponRoutine());
        }

        ApplySpeed();
        Debug.Log(gameObject.name + " escalated to " + tier);
    }


    private void ErraticDrop()
    {
        current_tier = GuardTier.Tier2;
        current_chase_bar = 0f;
        ApplySpeed();

        if (guard_mode != GuardMode.Patrol)
            static_search_routine = StartCoroutine(StaticScanRoutine());

        // alert every other guard in the building to go erratic
        EventBus.Publish(new ErraticAlertEvent());

        Debug.Log(gameObject.name +
            " dropped to Tier 2, alerting all guards.");
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
    private void UpdateChaseTracking()
    {
        if (can_see_player && player != null)
        {
            had_sight = true;
            player_last_position = player.position;

            Vector3 difference =
                player.position - player_previous_position;

            if (difference.sqrMagnitude > 0.01f)
                player_last_direction = difference.normalized;

            player_previous_position = player.position;

            // keep the bar topped off while chasing with sight
            current_chase_bar = chase_bar_max;
            return;
        }

        if (had_sight)
            had_sight = false;
    }


    // moves guard towards last known position (freezes at sight loss)
    private void ChasePlayer()
    {
        Vector3 target_destination;

        if (can_see_player)
            target_destination = player.position;
        else
            target_destination = player_last_position;

        if (Vector3.Distance(target_destination, current_destination) > 0.2f)
        {
            guard.SetDestination(target_destination);
            current_destination = target_destination;
        }

        if (can_see_player && player != null)
            FaceTarget(player.position);
        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();
        else
            FaceTarget(player_last_position);

        // reached last known position with no sight, begin search
        if (can_see_player == false && HasReachedDestination() &&
            active_routine == null)
        {
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    // walks to player_last_position regardless of chase bar logic
    private void Investigate()
    {
        if (Vector3.Distance(
            player_last_position, current_destination) > 0.2f)
        {
            guard.SetDestination(player_last_position);
            current_destination = player_last_position;
        }

        if (can_see_player && player != null)
            FaceTarget(player.position);
        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();
        else
            FaceTarget(player_last_position);

        if (HasReachedDestination() && active_routine == null)
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

        if (is_paused)
        {
            FaceTarget(target_point.position);

            pause_time -= Time.fixedDeltaTime;

            Vector3 to_target =
                (target_point.position - transform.position).normalized;
            bool facing_target =
                Quaternion.Angle(transform.rotation,
                YawFromDirection(to_target)) < 1f;

            if (pause_time <= 0f && facing_target)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }
            return;
        }

        FaceMovementDirection();

        if (HasReachedDestination())
        {
            is_paused = true;
            pause_time = pause_duration;
            guard.ResetPath();

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
        // only shoot when tier 4, weapon drawn, and player visible
        if (current_tier < GuardTier.Tier4 || is_drawing_weapon ||
            can_see_player == false || player == null)
            return;

        if (can_see_player)
            sight_timer += Time.fixedDeltaTime;
        else
            sight_timer = 0f;

        if (sight_timer < sight_delay)
            return;

        float distance =
            Vector3.Distance(transform.position, player.position);

        if (distance > shoot_range)
            return;

        if (Time.time < next_fire_time)
            return;

        next_fire_time = Time.time + fire_rate;

        Vector3 direction =
            (player.position - fire_point.position).normalized;

        Quaternion rotation =
            Quaternion.LookRotation(direction, Vector3.up);

        GameObject bullet_obj =
            Instantiate(bullet_prefab, fire_point.position, rotation);

        BulletMovement bullet =
            bullet_obj.GetComponent<BulletMovement>();

        if (bullet != null)
            bullet.Initialize(gameObject);
    }
    // ----- END SHOOT LOGIC ----- \\


    // ----- START ROTATION HELPERS ----- \\
    private void FaceMovementDirection()
    {
        if (guard.velocity.sqrMagnitude <= 0.01f)
            return;

        SmoothRotateTo(YawFromDirection(guard.velocity.normalized));
    }


    private void FaceTarget(Vector3 world_position)
    {
        Vector3 direction =
            (world_position - transform.position).normalized;
        SmoothRotateTo(YawFromDirection(direction));
    }


    private void SmoothRotateTo(Quaternion target)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            turn_speed * Time.fixedDeltaTime);
    }


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
        is_spotting = false;
        current_chase_bar = chase_bar_max;

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

        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        CancelSearchAndReturn();
        TierUp(GuardTier.Tier4);

        Debug.Log(gameObject.name +
            " heard gunshot, escalating to Tier 4.");
    }


    // another guard returned home after a failed chase
    // every guard that isn't already chasing drops to tier 2
    private void OnErraticAlertEvent(ErraticAlertEvent e)
    {
        // don't interrupt an active chase or demote from chase tier
        if (current_tier >= GuardTier.Tier3)
            return;

        // already erratic, nothing to do
        if (current_tier == GuardTier.Tier2)
            return;

        current_tier = GuardTier.Tier2;
        ApplySpeed();

        // static guards start scanning
        if (guard_mode != GuardMode.Patrol)
            static_search_routine = StartCoroutine(StaticScanRoutine());

        Debug.Log(gameObject.name +
            " received erratic alert, now Tier 2.");
    }
    // ----- END EVENT HANDLERS ----- \\


    // ----- START COLLISION HANDLERS ----- \\
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return; 
        }

        if (collision.CompareTag("Body") == false)
            return;

        // already chasing = instant game over
        if (current_tier >= GuardTier.Tier3)
        {
            EndGame();
            return;
        }

        is_spotting = false;
        current_chase_bar = chase_bar_max;
        TierUp(GuardTier.Tier3);

        if (is_catching == false)
            StartCoroutine(GracePeriodRoutine());
    }


    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        // Still touching after grace period expired = game over
        if (collision.CompareTag("Body") && is_catching == false &&
            current_tier >= GuardTier.Tier3)
        {
            EndGame();
        }
    }


    // handles bullet impact: damage, knockback, stagger
    private void TakeDamage(Collider bullet)
    {
        // compute knockback direction from bullet's travel
        Vector3 knockback_dir =
            (transform.position - bullet.transform.position).normalized;

        Destroy(bullet.gameObject);

        current_health--;

        if (current_health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // still alive - stagger the guard so the player can escape
        if (is_staggered == false)
            StartCoroutine(StaggerRoutine(knockback_dir));
    }


    private void EndGame()
    {
        Debug.Log("Player caught! Game Over.");
        EventBus.Publish(new GameOverEvent());
    }
    // ----- END COLLISION HANDLERS ----- \\


    // ----- START COROUTINES ----- \\
    private IEnumerator StaggerRoutine(Vector3 knockback_dir)
    {
        is_staggered = true;

        // cancel any in-progress search so the guard doesn't
        // resume mid-stagger
        CancelSearchAndReturn();
        is_spotting = false;
        guard.ResetPath();

        // knockback warp the guard backward on the navmesh
        Vector3 knockback_target =
            transform.position + knockback_dir * knockback_distance;

        if (NavMesh.SamplePosition(knockback_target,
            out NavMeshHit hit, knockback_distance, NavMesh.AllAreas))
        {
            // warp might not be the best TODO
            guard.Warp(hit.position);
        }

        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;

        // after recovering from stagger, escalate if not already
        if (guns_out)
            TierUp(GuardTier.Tier4);
        else if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);
    }


    // weapon draw delay, guard is tier 4 but can't shoot
    // until the animation/delay completes
    private IEnumerator DrawWeaponRoutine()
    {
        is_drawing_weapon = true;

        yield return new WaitForSeconds(weapon_draw_time);

        if (guard_weapon != null)
        {
            guard_weapon.SetActive(true);
            guards_sprite_renderer.sprite = lethal_guard_sprite;
        }

        is_drawing_weapon = false;
    }
    
    
    // ChasePlayer already brought us to player_last_position
    // run past in the direction the player was traveling
    // scan left/right anchored to the stopping position
    // return home
    private IEnumerator SearchAndReturnRoutine()
    {
        is_searching = true;
        guard.ResetPath();

        // overshoot in the direction the player was traveling
        if (player_last_direction.sqrMagnitude > 0.01f)
        {
            Vector3 predicted_point = player_last_position +
                player_last_direction * overshoot_distance;

            if (NavMesh.SamplePosition(predicted_point,
                out NavMeshHit hit, overshoot_distance,
                NavMesh.AllAreas))
            {
                guard.SetDestination(hit.position);

                while (HasReachedDestination() == false)
                {
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