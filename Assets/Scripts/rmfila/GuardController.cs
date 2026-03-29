using UnityEngine;
using UnityEngine.AI;
using static GameEvents;


public enum GuardTier
{
    // default calm state. once left, never returns here
    Tier1 = 1,
    // permanently alert, erratic patrol. lowest possible after any chase
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


public partial class GuardController : MonoBehaviour
{
    public GuardTier current_tier = GuardTier.Tier1;
    // whether or not to be lethal
    public bool guns_out = false;

    [Header("Guard Mode")]
    [Tooltip("Static = stands still. Patrol = walks between " +
    "point_a and point_b. StaticSearch = stands still but " +
    "scans back and forth.")]
    [SerializeField] private GuardMode guard_mode = GuardMode.Static;

    [Header("Health")]
    [SerializeField] private int max_health = 2;
    [SerializeField] private float stagger_duration = 2f;
    [SerializeField] private float knockback_distance = 3f;

    [Header("Patrol Settings")]
    [Tooltip("Patrol guard's starting point.")]
    [SerializeField] private Transform point_a;
    [Tooltip("Patrol guard's ending point.")]
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 1.5f;
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    [SerializeField] private float erratic_speed = 4.5f;

    [Header("Chase Bar")]
    [Tooltip("Time in seconds the bar takes to fill at max range.")]
    [SerializeField] private float chase_bar_max = 4f;
    [Tooltip("How fast the bar drains when player leaves sight.")]
    [SerializeField] private float chase_bar_decay = 1.5f;
    [Tooltip("Base fill rate per second when player is at the edge of " +
        "the vision cone. Exponentially scaled by proximity.")]
    [SerializeField] private float chase_bar_fill_rate = 3f;
    [Tooltip("How aggressively proximity speeds up the fill. " +
        "Higher = more punishing at close range.")]
    [SerializeField] private float proximity_exponent = 4f;

    [Header("Search Settings")]
    [Tooltip("How fast the guard turns in 1 second.")]
    [SerializeField] private float turn_speed = 180f;
    [Tooltip("How far the guard runs past the last know " +
        "location of the player")]
    [SerializeField] private float overshoot_distance = 2.5f;
    [Tooltip("How long the guard will still know where the" +
        " player is after losing sight of them.")]
    [SerializeField] private float pursuit_window = 1f;
    [Tooltip("How long the guard will search the player's last " +
        "known area.")]
    [SerializeField] private float investigate_timeout = 10f;

    [Header("Shooting Settings")]
    [Tooltip("Layers that block the guard from shooting.")]
    [SerializeField] private LayerMask wall_mask;
    [SerializeField] private LayerMask door_mask;
    [SerializeField] private GameObject bullet_prefab;
    [SerializeField] private Transform fire_point;
    [SerializeField] private float fire_rate = 1f;
    [Tooltip("How far the guard can shoot the player from.")]
    [SerializeField] private float shoot_range = 10f;
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

    [Header("Noise Settings")]
    [Tooltip("Must match expand_speed on your NoiseWave prefab.")]
    [SerializeField] private float noise_wave_expand_speed = 8f;

    // 0 means guard lost sight of player
    private float sight_loss_timer = 0f;
    // to predict how far the guard should travel
    // when chasing the player's last known visual
    private float player_estimated_speed = 0f;

    private int current_health;

    private bool is_staggered = false;

    private bool can_see_player = false;

    // distance to player when visible
    // used for fill rate scaling
    private float sight_distance = 0f;

    // the detect radius from the vision cone
    private float max_detect_radius;
    private float current_chase_bar = 0f;

    // true when the guard sees the player but the bar isn't full yet
    // guard freezes in place and tracks during this phase
    private bool is_spotting = false;

    // start position of the guard to return to
    private Vector3 start_position;
    // start rotation of the guard to return to
    private Quaternion start_rotation;
    // player last position and direction to try 
    // and investigate intelligently when guard
    // loses visual
    private Vector3 player_last_position;
    private Vector3 player_last_direction;
    // players position last frame for direction calculation
    private Vector3 player_previous_position;
    // normalized movement direction for search phase to overshoot
    private Vector3 last_chase_velocity;

    // the patrol guard's point a and b
    private Transform target_point;

    private Transform player;
    private NavMeshAgent guard;

    // use to toggle the gun on and off.
    private GameObject guard_weapon;
    // the guards vision cone mesh
    private VisionConeMesh vision_cone_mesh;

    // patrol pause
    private bool is_paused = false;
    private float pause_time = 0f;
    // used when the guard is returning to its
    // start position after a chase or something
    // this stops the guard from getting stuck somewhere
    // and never doing anything else
    private float return_timeout = 30f;
    // where we want the guard to go when chasing of some sorts
    private Vector3 current_destination;

    // if true, guard walks to player's last known position
    // stands there for a second, the runs search or return
    private bool is_investigating = false;
    private bool is_searching = false;
    private bool is_returning = false;
    private Coroutine active_routine = null;
    private Coroutine static_search_routine = null;

    // grace period so bumping a t1 guard isn't instant game over
    private bool is_catching = false;
    // next_fire_time < fire_rate == do not fire
    private float next_fire_time = 0f;
    // sight_timer < sight_delay == do not shoot the player yet
    private float sight_timer = 0f;
    private bool is_drawing_weapon = false;
    // used to determine if we lost sight this frame
    private bool had_sight_last_frame = false;
    // direction player was heading when guard lost sight
    // used for investigation
    private Vector3 sight_loss_direction;

    // how long we have been investigating for
    private float investigate_timer = 0f;
    
    // Blood particles
    [SerializeField] private ParticleSystem bloodEffectPrefab;


    // called by GuardVisionCone every detection tick
    // distance is how far the player is from the guard (0 when not visible)
    public void SpottedPlayer(bool value, float distance)
    {
        can_see_player = value;
        sight_distance = distance;

        // dont need to run if we cannot see the player
        if (can_see_player == false)
            return;

        // guard spotted player while investigating
        // switch to normal vision based chase
        if (is_investigating)
        {
            is_investigating = false;

            if (player != null)
            {
                player_previous_position = player.position;
                player_estimated_speed = 0f;
                player_last_direction = (player.position - transform.position).normalized;
            }
        }

        CancelAllRoutines();

        // if already chasing (bar was full), keep the tier and keep
        // updating last known position
        if (current_tier >= GuardTier.Tier3)
        {
            if (guns_out)
                TierUp(GuardTier.Tier4);

            return;
        }

        // not chasing yet - enter the spotting/freeze phase
        // the bar will fill in FixedUpdate
        if (is_spotting == false)
        {
            is_spotting = true;
            guard.ResetPath();
        }
    }


    public float GetChaseBarRatio()
    {
        if (chase_bar_max > 0f)
            return current_chase_bar / chase_bar_max;

        return 0f;
    }


    public bool IsDoorEligible()
    {
        if (is_staggered) 
            return false;

        // do not let patrolling, static, or static search guards open doors
        return 
            current_tier >= GuardTier.Tier3 || 
            is_investigating || 
            is_returning;
    }


    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
        EventBus.Subscribe<ErraticAlertEvent>(OnErraticAlertEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
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

        // agents will avoid one another and no longer collide/
        // fight over the same position of alert
        guard.avoidancePriority = Random.Range(1, 99);

        // firepoint is the "muzzle" of the gun
        guard_weapon = fire_point.parent.gameObject;
        guard_weapon.SetActive(false);
        guards_sprite_renderer.sprite = nonlethal_guard_sprite;

        vision_cone_mesh = GetComponentInChildren<VisionConeMesh>();

        if (vision_cone_mesh != null)
            max_detect_radius = vision_cone_mesh.GetDetectRadius();

        // player's body is a child of the player root, but the child
        // has the sphere collider
        GameObject player_object = GameObject.FindWithTag("Body");

        if (player_object != null)
        {
            player = player_object.transform;
            player_previous_position = player.position;
        }

        // set the guard in motion to point_a to begin patrol
        if (guard_mode == GuardMode.Patrol &&
            point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.speed = patrol_speed;
            guard.SetDestination(target_point.position);
        }

        if (guard_mode == GuardMode.StaticSearch)
            static_search_routine = StartCoroutine(StaticScanRoutine());
    }


    private void FixedUpdate()
    {
        // staggered? do nothing
        if (is_staggered)
            return;

        // can see the player but chase bar isnt full?
        if (is_spotting)
        {
            // let this handle tracking the player
            HandleSpotting();
            return;
        }

        // searching or returning from a search? do nothing
        if (is_searching || is_returning)
            return;

        if (current_tier <= GuardTier.Tier2)
        {
            // keep patrolling between point a and b if on patrol mode
            if (guard_mode == GuardMode.Patrol)
                Patrol();

            // otherwise, just do what you were doing
            return;
        }

        // lethal/nonlethal chasing
        if (current_tier >= GuardTier.Tier3)
        {
            // lost sight of player
            if (is_investigating)
            {
                Investigate();
                // attempt to shoot at the player if possible
                ShootAtPlayer();
                return;
            }

            // update the bar for if player is seen or not
            UpdateChaseTracking();
            // chase player if possible
            ChasePlayer();
            ShootAtPlayer();
        }
    }


    private void FaceMovementDirection()
    {
        // needs to be a significant enough change in movement
        if (guard.velocity.sqrMagnitude <= 0.01f)
            return;

        // pans the guard to face the right way
        SmoothRotateTo(YawFromDirection(guard.velocity.normalized));
    }


    private void FaceTarget(Vector3 world_position)
    {
        // pans the guard to face the target position
        // used for spotting and investigating
        Vector3 direction = (world_position - transform.position).normalized;
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
        // gets the angle in degrees from a direction vector, then converts
        // it to a quaternion. used for all facing of the guard
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, angle, 0f);
    }
}


// credits
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html
