using System.Collections;
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
    public bool guns_out = false;

    [Header("Guard Mode")]
    [Tooltip("Static = stands still. Patrol = walks between " +
    "point_a and point_b. StaticSearch = stands still but " +
    "scans back and forth.")]
    [SerializeField] private GuardMode guard_mode = GuardMode.Static;

    [Header("Health")]
    [SerializeField] private int max_health = 2;
    [SerializeField] private float stagger_duration = 1f;
    [SerializeField] private float knockback_distance = 0.5f;

    [Header("Patrol Settings")]
    [Tooltip("Patrol guard's starting point.")]
    [SerializeField] private Transform point_a;
    [Tooltip("Patrol guard's ending point.")]
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 1.5f;
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    // bumped from 4.5 - guards are now faster and more persistent
    [SerializeField] private float erratic_speed = 5.5f;

    [Header("Chase Bar")]
    [Tooltip("Time in seconds the bar takes to fill at max range.")]
    [SerializeField] private float chase_bar_max = 4f;
    [Tooltip("How fast the bar drains when player leaves sight.")]
    // slowed decay so the bar doesn't drain so fast
    [SerializeField] private float chase_bar_decay = 1.0f;
    [Tooltip("Base fill rate per second when player is at the edge of " +
        "the vision cone. Exponentially scaled by proximity.")]
    // bumped from 4.5 - fills faster now
    [SerializeField] private float chase_bar_fill_rate = 6f;
    [Tooltip("How aggressively proximity speeds up the fill. " +
        "Higher = more punishing at close range.")]
    [SerializeField] private float proximity_exponent = 4f;

    [Header("Search Settings")]
    [Tooltip("How fast the guard turns in degrees per second.")]
    [SerializeField] private float turn_speed = 200f;
    [Tooltip("How far the guard runs past the last known " +
        "location of the player")]
    // bumped from 2.5 - guards commit further around corners
    [SerializeField] private float overshoot_distance = 3.5f;
    [Tooltip("How long the guard still knows where the player " +
        "is after losing sight of them.")]
    // bumped from 1f - guards don't give up after 1 second anymore
    [SerializeField] private float pursuit_window = 2f;
    [Tooltip("How long the guard will search the player's last " +
        "known area.")]
    // bumped from 10f
    [SerializeField] private float investigate_timeout = 15f;

    [Header("Shooting Settings")]
    [Tooltip("Layers that block the guard from shooting.")]
    [SerializeField] private LayerMask wall_mask;
    [SerializeField] private LayerMask door_mask;
    [SerializeField] private GameObject bullet_prefab;
    [SerializeField] private Transform fire_point;
    [SerializeField] private float fire_rate = 1f;
    [Tooltip("How far the guard can shoot the player from.")]
    // bumped from 7 - longer threat range
    [SerializeField] private float shoot_range = 9f;
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

    // sight tracking
    private float sight_loss_timer = 0f;
    private bool can_see_player = false;
    private float sight_distance = 0f;
    private float max_detect_radius;
    private float current_chase_bar = 0f;

    // spotting phase (bar filling before a chase)
    private bool is_spotting = false;
    // one-shot flag: only call ResetPath once when spotting starts, not every frame
    private bool spotting_reset_done = false;

    // health
    private int current_health;
    private bool is_staggered = false;

    // position memory
    private Vector3 start_position;
    private Quaternion start_rotation;
    private Vector3 player_last_position;
    private Vector3 player_last_direction;
    // player position last frame - only accurate when the guard can see the player
    private Vector3 player_previous_position;
    // direction guard was heading toward player when sight was lost
    private Vector3 sight_loss_direction;
    // the actual world position where sight was lost (used for capping overshoot)
    private Vector3 actual_sight_loss_position;
    // flag: was the guard seeing the player last FixedUpdate frame
    private bool had_sight_last_frame = false;

    // patrol
    private Transform target_point;
    private bool is_paused = false;
    private float pause_time = 0f;

    // NavMesh references
    private Transform player;
    private NavMeshAgent guard;
    private GameObject guard_weapon;
    private VisionConeMesh vision_cone_mesh;

    // state flags
    private bool is_investigating = false;
    private bool is_searching = false;
    private bool is_returning = false;
    private bool is_door_paused = false;
    private bool is_catching = false;
    private bool is_drawing_weapon = false;

    // coroutine handles
    private Coroutine active_routine = null;
    private Coroutine static_search_routine = null;

    // destination tracking
    private Vector3 current_destination;
    private float return_timeout = 15f;

    // investigation timer
    private float investigate_timer = 0f;

    // shooting timers
    private float next_fire_time = 0f;
    private float sight_timer = 0f;

    // stuck detection when chasing without vision
    // if the guard hasn't moved 0.5 units in this many seconds, begin searching
    private float chase_stuck_timer = 0f;
    private Vector3 chase_stuck_check_pos;
    private const float CHASE_STUCK_THRESHOLD = 4f;

    // Blood particles
    [SerializeField] private ParticleSystem bloodEffectPrefab;

    // called by GuardVisionCone every detection tick
    public void SpottedPlayer(bool value, float distance)
    {
        can_see_player = value;
        sight_distance = distance;

        if (!can_see_player)
            return;

        // guard spotted player while investigating - switch to vision-based tracking
        if (is_investigating)
        {
            is_investigating = false;
            investigate_timer = 0f;
            if (player != null)
            {
                // FIX: reset previous position so tracking direction starts accurate
                player_previous_position = player.position;
                player_last_position = player.position;
                player_last_direction = (player.position - transform.position).normalized;
            }
        }

        // already in active chase - cancel search/return and keep chasing
        if (current_tier >= GuardTier.Tier3)
        {
            CancelAllRoutines();
            if (guns_out)
                TierUp(GuardTier.Tier4);
            return;
        }

        // Tier1/2 - start filling the chase bar
        if (!is_spotting)
        {
            is_spotting = true;
            spotting_reset_done = false;
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
        return guard.velocity.sqrMagnitude > 0.05f ||
               current_tier >= GuardTier.Tier3 ||
               is_investigating ||
               is_returning;
    }

    public bool IsReturning() => is_returning;

    public void PauseNavigation(float duration)
    {
        if (is_door_paused) return;
        StartCoroutine(PauseNavigationRoutine(duration));
    }

    private IEnumerator PauseNavigationRoutine(float duration)
    {
        is_door_paused = true;
        Vector3 saved = current_destination;
        guard.isStopped = true;
        yield return new WaitForSeconds(duration);
        guard.isStopped = false;
        // FIX: original code skipped this when is_searching = true,
        // which caused guards to freeze mid-search after passing through a door.
        // now always restore destination when not staggered.
        if (!is_staggered)
            SetGuardDestination(saved);
        is_door_paused = false;
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
        guard.stoppingDistance = 0.3f;
    }

    private void Start()
    {
        current_health = max_health;
        start_position = transform.position;
        start_rotation = transform.rotation;
        guard.avoidancePriority = Random.Range(1, 99);
        guard_weapon = fire_point.parent.gameObject;
        guard_weapon.SetActive(false);
        guards_sprite_renderer.sprite = nonlethal_guard_sprite;
        vision_cone_mesh = GetComponentInChildren<VisionConeMesh>();
        if (vision_cone_mesh != null)
            max_detect_radius = vision_cone_mesh.GetDetectRadius();

        GameObject player_object = GameObject.FindWithTag("Body");
        if (player_object != null)
        {
            player = player_object.transform;
            // FIX: initialize both to the player's current position so the
            // first detection doesn't produce a garbage direction/speed
            player_previous_position = player.position;
            player_last_position = player.position;
        }

        chase_stuck_check_pos = transform.position;

        if (guard_mode == GuardMode.Patrol && point_a != null && point_b != null)
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
        if (is_staggered)
            return;

        if (is_spotting)
        {
            HandleSpotting();
            return;
        }

        if (is_searching || is_returning)
            return;

        if (current_tier <= GuardTier.Tier2)
        {
            if (guard_mode == GuardMode.Patrol)
                Patrol();
            return;
        }

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