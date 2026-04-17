using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;

// different type of guards
// static search guards differ from static
// in that they scan 180 degrees back and forth
// at their starting position.
public enum GuardMode 
{ 
    Static, 
    Patrol, 
    StaticSearch 
}


public partial class GuardController : MonoBehaviour
{
    // determines if guard has sight of the player
    // or the palyer is in the guard's vision cone
    public bool can_see_player = false;
    // determines if guard is alerted or not
    public bool is_alerted = false;
    // determines if guard has weapon drawn or not
    public bool guns_out = false;

    // main routine logic for patrol, chase, etc partial classes require it
    public Coroutine active_routine = null;

    public Transform player;
    public NavMeshAgent guard;

    [Header("Guard Mode")]
    [SerializeField] private GuardMode guard_mode = GuardMode.Static;

    [Header("Health")]
    [SerializeField] private int max_health = 5;

    [Header("Patrol")]
    [SerializeField] private Transform point_a;
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 1.5f;
    [Tooltip("How long the guard pauses at its target " +
        "point before moving to the next target.")]
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Static")]
    [SerializeField] private float turn_speed = 180f;

    [Header("Chase")]
    [SerializeField] private float chase_speed = 3.5f;
    [Tooltip("How far the player has to travel to make " +
        "the guard go back to its starting position")]
    [SerializeField] private float give_up_distance = 15f;
    [SerializeField] private float min_chase_duration = 4f;

    [Header("Shooting")]
    [SerializeField] private LayerMask wall_mask;
    [SerializeField] private LayerMask door_mask;
    [SerializeField] private GameObject bullet_prefab;
    [SerializeField] private Transform fire_point;
    [SerializeField] private float fire_rate = 1f;
    [Tooltip("The max range the guard's can shoot within.")]
    [SerializeField] private float shoot_range = 5f;
    [Tooltip("How long the guard's take to react to the player and shoot.")]
    [SerializeField] private float sight_delay = 0.5f;
    [Tooltip("How long the guard's take to draw " +
        "their gun to shoot the player.")]
    [SerializeField] private float weapon_draw_time = 1f;
    [SerializeField] private float knockback_distance = 0.5f;
    [SerializeField] private float knockback_duration = 0.2f;
    [Tooltip("How long the guard remains stationary after being shot.")]
    [SerializeField] private float stagger_duration = 1f;

    [Header("Juice")]
    [SerializeField] SpriteRenderer guards_sprite_renderer;
    [SerializeField] Sprite nonlethal_guard_sprite;
    [SerializeField] Sprite lethal_guard_sprite;
    [SerializeField] private ParticleSystem bloodEffectPrefab;

    [Header("Noise")]
    [Tooltip("How long the guard takes to hear a gunshot. " +
        "Needs to match the exact same as the NoiseWave prefab.")]
    [SerializeField] private float noise_wave_expand_speed = 8f;

    [SerializeField] private bool start_alerted = false;

    // determines if the guard is staggered by the player
    private bool is_staggered = false;

    private int current_health;

    // determines if the guard is pulling their weapon out
    // small delay happens
    private bool is_drawing_weapon = false;

    // determines if the door is pausing the guards nav mesh
    private bool is_waiting_for_door = false;

    // determines if guard lost sight of player and is returning
    // back to their starting position. This really cannot happen
    private bool is_returning = false;

    // determines if the guard gives the player a graceperiod
    // on direct contact with un unaware guard
    private bool is_catching = false;

    // starting position and rotation of the guard
    private Vector3 start_position;
    private Quaternion start_rotation;

    // the position the guard will be actively chasing of the player
    public Vector3 player_last_position;

    // the target destination for a patrol guard
    private Transform target_point;

    // the guards weapon to activate and deactivate shooting capabilities
    private GameObject guard_weapon;

    // used to cache the in route destination when the route is paused
    private Vector3 current_destination;

    // safety that ensures guards won't chase nothing forever
    private const float return_timeout = 15f;

    // how long the guard has chased
    private float chase_timer = 0f;

    private float next_fire_time = 0f;

    // how long the guard has been spotting the player. determines
    // when they can shoot after seeing player for first time
    private float sight_timer = 0f;

    // used to get a guard unstuck in chasing
    private Vector3 stuck_check_position;
    private float stuck_timer = 0f;
    private const float stuck_check_interval = 2.5f;
    private const float stuck_move_threshold = 0.4f;


    // used to determine if these corotuines are running
    private Coroutine static_scan_routine = null;
    private Coroutine draw_weapon_routine = null;
    private Coroutine stagger_routine = null;


    // determines if we can see the player
    // if so, update its last position it saw
    public void SpottedPlayer(bool can_see)
    {
        can_see_player = can_see;

        if (can_see_player == true)
        {
            if (player != null) 
                player_last_position = player.position;

            if (is_alerted == false) 
                Alert();
        }
    }

    // updates the player's last known position, sets the guards speed to
    // chase, and draws the guards weapon. this primes the guard for chasing
    public void Alert()
    {
        chase_timer = 0f;

        stuck_timer = 0f;
        stuck_check_position = transform.position;

        is_alerted = true;

        // only cancel the active chase routine or static scan routine
        if (active_routine != null) 
        { 
            StopCoroutine(active_routine); 
            active_routine = null; 
        }

        if (static_scan_routine != null) 
        { 
            StopCoroutine(static_scan_routine); 
            static_scan_routine = null; 
        }

        // cancelled any return routine
        is_returning = false;

        guard.speed = chase_speed;

        if (player != null) 
            player_last_position = player.position;

        if (guns_out == false && is_drawing_weapon == false) 
            draw_weapon_routine = StartCoroutine(DrawWeaponRoutine());
    }


    // determines if guard is able to open door
    public bool IsDoorEligible()
    {
        // cannot open door if staggered
        if (is_staggered == true) 
            return false;

        // returns true if guard has moved significantly,
        // is alert, or is returning
        return is_alerted == true || is_returning == true;
    }


    // pauses the guard's nav for a duration
    public void PauseNavigation(float duration)
    {
        // door is pausing the guards movement currently
        if (is_waiting_for_door == true) 
            return;

        // guards movement isnt already paused, so pause it
        StartCoroutine(PauseNavigationRoutine(duration));
    }


    private IEnumerator PauseNavigationRoutine(float duration)
    {
        // actively pausing the guard's movement
        is_waiting_for_door = true;

        // save the current destination the guard was in to return to
        // after unpausing
        Vector3 saved = current_destination;

        // actually pause the guard's navmesh agent
        guard.isStopped = true;

        // pause the guard for the duration specified
        yield return new WaitForSeconds(duration);

        // unpause the guard's navmesh agent
        guard.isStopped = false;

        // set the guard to the saved destination,
        // unless staggered in the process
        if (is_staggered == false) 
            SetGuardDestination(saved);

        // guard is no longer being paused by the door
        is_waiting_for_door = false;
    }


    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
    }


    private void Start()
    {
        guard = GetComponentInChildren<NavMeshAgent>();

        // we manually update the navmesh agent's rotation
        guard.updateRotation = false;
        // tell the navmesh agent to not auto align the character
        // since we manually adjust the rotation, we don't want the
        // agent to intefere with my set orientation
        guard.updateUpAxis = false;

        // how far away the guard stops from the player
        guard.stoppingDistance = 1f;

        current_health = max_health;

        start_position = transform.position;
        start_rotation = transform.rotation;

        // assign a guard a value for its priority so they don't collide
        // or intefere with other guards
        guard.avoidancePriority = Random.Range(1, 99);

        guard_weapon = fire_point.parent.gameObject;
        // disable the gun be default
        guard_weapon.SetActive(false);
        // set the guards default starting sprite be be non_lethal
        guards_sprite_renderer.sprite = nonlethal_guard_sprite;

        // where the player is being rendered
        GameObject player_obj = GameObject.FindWithTag("Body");

        if (player_obj != null)
        {
            player = player_obj.transform;
            player_last_position = player.position;
        }

        if (guard_mode == GuardMode.Patrol
            && point_a != null && point_b != null)
        {
            target_point = point_a;
            active_routine = StartCoroutine(PatrolRoutine());
        }

        if (guard_mode == GuardMode.StaticSearch)
            static_scan_routine = StartCoroutine(StaticScanRoutine());

        if (start_alerted)
            Alert();
    }

    private void FixedUpdate()
    {
        // staggered or returning back to start, do nothing
        if (is_staggered == true || is_returning == true)
            return;

        // not alerted, patrol is handled by its coroutine
        if (is_alerted == false)
            return;

        stuck_timer += Time.fixedDeltaTime;

        if (stuck_timer >= stuck_check_interval)
        {
            stuck_timer = 0f;

            if (Vector3.Distance(
                transform.position, 
                stuck_check_position) < stuck_move_threshold)
            {
                // guard hasn't moved enough, resync navmesh and force repath
                guard.Warp(transform.position);
                guard.SetDestination(player_last_position);
                current_destination = player_last_position;
            }

            stuck_check_position = transform.position;
        }

        // update the last known player position every frame
        // this gives the guards up to date positions of the player to chase
        if (player != null) 
            player_last_position = player.position;

        if (is_alerted)
            chase_timer += Time.fixedDeltaTime;

        ChasePlayer();
        ShootAtPlayer();
    }


    // face the current direction the guard is moving smoothly
    public void FaceMovementDirection()
    {
        if (guard.velocity.sqrMagnitude <= 0.01f) 
            return;

        SmoothRotateTo(RotationFromDirection(guard.velocity.normalized));
    }


    // smoothly rotates to the target the guard is chasing
    public void FaceTarget(Vector3 world_position)
    {
        SmoothRotateTo(
            RotationFromDirection(
                (world_position - transform.position).normalized
            )
        );
    }


    // smoothly rotate two target input rotation
    private void SmoothRotateTo(Quaternion target)
    {
        transform.rotation = 
            Quaternion.RotateTowards(
                transform.rotation, 
                target, 
                turn_speed * Time.fixedDeltaTime
            );
    }


    // converts a direction to a rotation in only the Y axis
    private Quaternion RotationFromDirection(Vector3 direction)
    {
        return 
            Quaternion.Euler(
                0f, 
                Mathf.Atan2(
                    direction.x, 
                    direction.z
                ) * Mathf.Rad2Deg, 
                0f
            );
    }


    // determines if the guard has reached its set destination
    public bool HasReachedDestination()
    {
        if (guard.pathStatus == NavMeshPathStatus.PathPartial) 
            return false;

        // return true is no path is pending and
        // which in range of stopping distance
        return 
            guard.pathPending == false 
            && guard.remainingDistance <= guard.stoppingDistance;
    }


    // sets the guards destination and saves the current destination
    // for recovering later
    public void SetGuardDestination(Vector3 target)
    {
        // checks if the target destination is a siginificant enough
        // change in distance from where we are currently heading
        if (Vector3.Distance(target, current_destination) > 0.2f)
        {
            guard.SetDestination(target);
            current_destination = target;
        }
    }


    // measures the total distance collected from multiple rays
    // casting from a given direction. This is used to determine which
    // direction has more "open space" than the other. open space is
    // determined by the distance a ray can shoot out without colliding
    // with the wall_mask.
    private float MeasureOpenness(Vector3 cast_direction)
    {
        float spread = 60f;
        int ray_count = 3;
        float ray_length = 10f;

        float total = 0f;

        // loops for all rays and cast them across
        // the spread angle equally. similar to vision cone
        for (int i = 0; i < ray_count; i++)
        {
            // converts the loop progress into a percentage of completion
            // so i=0 -> 0.0, i=1 -> 0.5, i=2 -> 1.0
            float angle_progress = i / (float)(ray_count - 1);

            // map the progress to an actual angle
            // so if we have 60 degrees, i=0 -> -30, i=1 -> 0, i=2 -> 30
            float spread_offset_in_degrees = 
                Mathf.Lerp(-spread / 2f, spread / 2f, angle_progress);

            // get the rays direction at the angle offset calculated
            // creates the fan shape
            Vector3 direction = Quaternion.Euler(
                0f, 
                spread_offset_in_degrees, 
                0f
            ) * cast_direction.normalized;

            // cast the ray out and record how far the hit distance is
            if (Physics.Raycast(
                transform.position,
                direction,
                out RaycastHit hit,
                ray_length, 
                wall_mask) == true)
            {
                total += hit.distance;
            }

            // or record the ray's max length if it hit nothing
            else 
               total += ray_length;
        }

        // this returns the average length across rays higher value
        // means more open and the direction we want to rotate
        return total / ray_count;
    }
}
