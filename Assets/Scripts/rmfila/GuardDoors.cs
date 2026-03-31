using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GuardDoors : MonoBehaviour
{
    [Header("Door Settings")]
    public float swingAngle = 90f;
    public float swingSpeed = 3f;
    public BoxCollider doorCollider;

    [Header("Player Interaction")]
    public float interactRange = 0.75f;

    [Header("Guard Interaction")]
    [SerializeField] private float guard_detect_range = 0.6f;
    [Tooltip("Delay before door begins swinging. Guard is frozen this entire time.")]
    [SerializeField] private float guard_open_delay = 0.25f;
    [Tooltip("Extra buffer after door starts opening so guard doesn't clip through.")]
    [SerializeField] private float swing_buffer = 0.35f;
    [SerializeField] private LayerMask guard_layer;
    [Tooltip("Seconds before a non-returning guard's door auto-closes. Set 0 to disable.")]
    [SerializeField] private float auto_close_delay = 8f;

    [Header("Sound Blocking")]
    [SerializeField] private Collider sound_blocker;

    [Header("Obstacle")]
    [SerializeField] private NavMeshObstacle door_obstacle;

    private Transform player;
    private bool isOpen = false;
    private bool guard_opening = false;
    private float closedAngle;
    private float targetAngle;
    private GuardController last_guard_opener = null;
    private bool watching_for_close = false;
    private float poll_timer = 0f;
    private const float POLL_INTERVAL = 0.1f;
    private Coroutine auto_close_routine = null;

    void Start()
    {
        closedAngle = transform.localEulerAngles.y;
        targetAngle = closedAngle;
        if (door_obstacle != null)
            door_obstacle.enabled = false;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject body = GameObject.FindWithTag("Body");
            if (body != null) player = body.transform;
        }

        if (player != null)
        {
            Vector3 closest = doorCollider.ClosestPoint(player.position);
            if (Vector3.Distance(closest, player.position) <= interactRange &&
                Input.GetKeyDown(KeyCode.E))
            {
                if (isOpen) CloseDoor();
                else OpenDoor(player);
            }
        }

        if (!isOpen && !guard_opening)
        {
            poll_timer += Time.deltaTime;
            if (poll_timer >= POLL_INTERVAL)
            {
                poll_timer = 0f;
                CheckForGuard();
            }
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0f, newY, 0f);
    }

    private void CheckForGuard()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, guard_detect_range, guard_layer);
        foreach (Collider col in nearby)
        {
            GuardController gc = col.GetComponentInParent<GuardController>();
            if (gc != null && gc.IsDoorEligible())
            {
                StartCoroutine(GuardOpenRoutine(gc));
                return;
            }
        }
    }

    private IEnumerator GuardOpenRoutine(GuardController gc)
    {
        guard_opening = true;
        float total_pause = guard_open_delay + swing_buffer;
        gc.PauseNavigation(total_pause);

        yield return new WaitForSeconds(guard_open_delay);

        if (!isOpen)
        {
            OpenDoor(gc.transform);
        }

        guard_opening = false;
    }

    public void OpenDoor(Transform opener)
    {
        if (isOpen) return;
        Vector3 localPos = transform.InverseTransformPoint(opener.position);
        float direction = localPos.x > 0 ? 1f : -1f;
        targetAngle = closedAngle + swingAngle * direction;
        isOpen = true;
        if (sound_blocker != null) sound_blocker.enabled = false;
        if (door_obstacle != null) door_obstacle.enabled = true;
    }

    public void CloseDoor()
    {
        if (!isOpen) return;
        targetAngle = closedAngle;
        isOpen = false;
        if (sound_blocker != null) sound_blocker.enabled = true;
        if (door_obstacle != null) door_obstacle.enabled = false;
    }
}