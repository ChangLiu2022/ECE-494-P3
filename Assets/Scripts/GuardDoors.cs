using System.Collections;
using UnityEngine;

public class GuardDoors : MonoBehaviour
{
    public float interactRange = 0.75f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;
    public BoxCollider doorCollider;

    [SerializeField] private float guard_detect_range = 1.5f;
    [SerializeField] private float guard_open_delay = 0.4f;
    [SerializeField] private LayerMask guard_layer;
    [SerializeField] private Collider sound_blocker;

    private Transform player;
    private bool isOpen = false;
    // if a guard is opening the door we don't
    // want to start another routine to open the door
    private bool guard_opening = false;
    private float closedAngle;
    private float targetAngle;

    void Start()
    {
        closedAngle = transform.localEulerAngles.y;
        targetAngle = closedAngle;
    }

    void Update()
    {
        // moving this to update and setting the check to only update it
        // if the player is null, so we dont do this every time
        if (player == null)
        {
            GameObject body = GameObject.FindWithTag("Body");

            if (body == null) 
                return;

            player = body.transform;
        }

        Vector3 closestPoint = doorCollider.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen == true)
                CloseDoor();
            else
                OpenDoor(player);
        }

        if (isOpen == false && guard_opening == false)
        {
            // find all colliders in a sphere around the door
            // guard detect range is how close the guards need to be
            Collider[] near_check = Physics.OverlapSphere(
                transform.position, guard_detect_range, guard_layer);

            // loop through all nearby guuards
            foreach (Collider collider in near_check)
            {
                GuardController guard = 
                    collider.GetComponentInParent<GuardController>();

                // check if the guard can open doors
                if (guard != null && guard.IsDoorEligible())
                {
                    StartCoroutine(GuardOpenRoutine(collider.transform));
                    break;
                }
            }
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }


    private IEnumerator GuardOpenRoutine(Transform guard)
    {
        // if a guard is already opening the door
        // we don't want to start another routine
        guard_opening = true;

        // how long the guard takes to open the door after they are in range
        yield return new WaitForSeconds(guard_open_delay);

        // open the door if its not open already
        OpenDoor(guard);

        guard_opening = false;
    }


    public void OpenDoor(Transform opener)
    {
        if (isOpen) return;

        Vector3 localPos = transform.InverseTransformPoint(opener.position);
        float direction = localPos.x > 0 ? 1f : -1f;

        targetAngle = closedAngle + (swingAngle * direction);
        isOpen = true;

        // if the door has a sound blocker disable it when
        // the door is open so sound can pass through
        if (sound_blocker != null) 
            sound_blocker.enabled = false;
    }

    public void CloseDoor()
    {
        if (!isOpen) return;

        targetAngle = closedAngle;
        isOpen = false;

        if (sound_blocker != null) 
            sound_blocker.enabled = true;
    }
}
