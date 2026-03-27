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
    [SerializeField] private float pursuit_distance = 5f;
    [SerializeField] private float pursuit_window = 1f;

    [Header("Shooting Settings")]
    [SerializeField] private LayerMask wall_mask;
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

    private float sight_loss_timer = 0f;
    private float player_estimated_speed = 0f;
    private int current_health;

    // tells if the guard was shot and hit
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
    private Vector3 last_chase_velocity;
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
    private bool had_sight_last_frame = false;
    private Vector3 sight_loss_direction;


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
            if (guns_out)
                TierUp(GuardTier.Tier4);

            return;
        }

        // not chasing yet - enter the spotting/freeze phase
        // the bar will fill in FixedUpdate
        if (is_spotting == false)
        {
            is_spotting = true;
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
        // agents will avoid one another and no longer collide/
        // fight over the same position of alert
        guard.avoidancePriority = Random.Range(1, 99);

        // firepoint is the "muzzle" of the gun
        guard_weapon = fire_point.parent.gameObject;
        guard_weapon.SetActive(false);
        guards_sprite_renderer.sprite = nonlethal_guard_sprite;

        // cache the detect radius for normalizing distance in fill calc
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

        // searching or returning from a search? do nothing
        if (is_searching || is_returning)
            return;

        // can see the player but chase bar isnt full?
        if (is_spotting)
        {
            // let this handle tracking the player
            HandleSpotting();
            return;
        }

        if (current_tier <= GuardTier.Tier2)
        {
            if (guard_mode == GuardMode.Patrol)
                Patrol();

            return;
        }

        // lethal/nonlethal chasing
        if (current_tier >= GuardTier.Tier3)
        {
            // lost sight of player
            if (is_investigating)
            {
                // investigate
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
        if (guard.velocity.sqrMagnitude <= 0.01f)
            return;

        SmoothRotateTo(YawFromDirection(guard.velocity.normalized));
    }


    private void FaceTarget(Vector3 world_position)
    {
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
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, angle, 0f);
    }
}

// credits
// https://docs.unity3d.com/2022.2/Documentation/Manual/Rigidbody2D-Kinematic.html
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html
