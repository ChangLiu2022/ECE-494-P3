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
    [Tooltip("Delay before door begins swinging. " +
        "Guard is frozen this entire time.")]
    [SerializeField] private float guard_open_delay = 0.25f;
    [Tooltip("Extra buffer after door starts opening " +
        "so guard doesn't clip through.")]
    [SerializeField] private float swing_buffer = 0.35f;
    [SerializeField] private LayerMask guard_layer;

    [Header("Sound Blocking")]
    [SerializeField] private Collider sound_blocker;

    [Header("Obstacle")]
    [SerializeField] private NavMeshObstacle door_obstacle;

    [SerializeField] private bool guard_only_door = false;

    private Transform player;
    private bool isOpen = false;
    private bool guard_opening = false;
    private float closedAngle;
    private float targetAngle;
    private float timer = 0f;


    void Start()
    {
        closedAngle = transform.localEulerAngles.y;
        targetAngle = closedAngle;
    }


    void Update()
    {
        if (!guard_only_door)
        {
            if (player == null)
            {
                GameObject body = GameObject.FindWithTag("Body");

                if (body != null)
                    player = body.transform;
            }

            if (player != null)
            {
                // get the distance from the player to the doors closest point
                Vector3 closest = doorCollider.ClosestPoint(player.position);

                // if the player is close enough and presses E, open/close doors
                if (Vector3.Distance(closest, player.position) <= interactRange
                    && Input.GetKeyDown(KeyCode.E))
                {
                    // doors already open and pressed E, close it
                    if (isOpen == true)
                        CloseDoor();

                    // door already closed and pressed E, open it
                    else
                        OpenDoor(player);
                }
            }
        }

        // if the doors are closed and a guard isnt in the process of
        // opening it, check for nearby guards on a fixed update
        // stops doors from checking for guards every frame -- performance
        if (isOpen == false && guard_opening == false)
        {
            timer += Time.deltaTime;

            if (timer >= 0.1f)
            {
                timer = 0f;
                CheckForGuard();
            }
        }

        float currentY = transform.localEulerAngles.y;

        float newY = Mathf.LerpAngle(
            currentY, 
            targetAngle, 
            Time.deltaTime * swingSpeed
        );

        // swing the door open
        transform.localEulerAngles = new Vector3(0f, newY, 0f);
    }


    private void CheckForGuard()
    {
        // check for guards in a small radius around the door
        Collider[] nearby = Physics.OverlapSphere(
            transform.position, 
            guard_detect_range, 
            guard_layer
        );

        // loop over all detected colliders in the sphere
        // and check if they have a guard controller component
        foreach (Collider col in nearby)
        {
            GuardController guard = 
                col.GetComponentInParent<GuardController>();

            // if found a guard controller and guard is able to open the door
            if (guard != null && guard.IsDoorEligible())
            {
                // open the door and pause the guard's nav while it opens
                StartCoroutine(GuardOpenRoutine(guard));
                return;
            }
        }
    }


    private IEnumerator GuardOpenRoutine(GuardController guard)
    {
        guard_opening = true;

        // allows timing to be timed for each part, stopping the guard
        // when they get to the door, and stopping the guard while the
        // door is swinging open so they don't clip through it
        float total_pause = guard_open_delay + swing_buffer;
        guard.PauseNavigation(total_pause);

        yield return new WaitForSeconds(guard_open_delay);

        if (isOpen == false)
        {
            OpenDoor(guard.transform);
        }

        guard_opening = false;
    }

    public void OpenDoor(Transform opener)
    {
        if (isOpen == true) 
            return;

        // which side of the door the opener is on, determines which way the
        // door swings open
        Vector3 localPos = transform.InverseTransformPoint(opener.position);

        float direction;

        // if the opener is on the right side the targetAngle will be
        // positive meaning the swingAngle is added to the closed angle
        if (localPos.x > 0)
        {
            direction = 1f;
        }

        // otherwise, the opener is on the left, targetAngle is negative,
        // meaning swingAngle is subtracted from the closed angle
        else
        {
            direction = -1f;
        }

        targetAngle = closedAngle + swingAngle * direction;

        isOpen = true;

        // door opened, no longer block sound
        if (sound_blocker != null) 
            sound_blocker.enabled = false;

        // door opened, now an obstacle
        if (door_obstacle != null) 
            door_obstacle.enabled = true;
    }

    public void CloseDoor()
    {
        if (isOpen == false) 
            return;

        // go to closed angle
        targetAngle = closedAngle;

        isOpen = false;

        if (sound_blocker != null) 
            sound_blocker.enabled = true;

        if (door_obstacle != null) 
            door_obstacle.enabled = false;
    }
}