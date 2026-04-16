using UnityEngine;

public class Door : MonoBehaviour
{
    public float interactRange = 0.75f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;
    public BoxCollider doorCollider;

    private Transform player;
    private bool isOpen = false;
    private float closedAngle;
    private float targetAngle;

    void Start()
    {
        player = GameObject.FindWithTag("Body").transform;
        closedAngle = transform.localEulerAngles.y;
        targetAngle = closedAngle;
    }

    void Update()
    {
        if (player == null || doorCollider == null) return; 
        Vector3 closestPoint = doorCollider.ClosestPoint(player.position);
        float distance = Vector3.Distance(closestPoint, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen)
            {
                targetAngle = closedAngle;
            }
            else
            {
                Vector3 localPos = transform.InverseTransformPoint(player.position);
                float direction = localPos.x > 0 ? -1f : 1f;
                targetAngle = closedAngle + (swingAngle * direction);
            }

            isOpen = !isOpen;
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }
}