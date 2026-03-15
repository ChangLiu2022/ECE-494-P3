using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour
{
    public float interactRange = 2f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;

    private Transform player;
    private NavMeshObstacle obstacle;
    private bool isOpen = false;
    private float targetAngle = 0f;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        obstacle = GetComponentInChildren<NavMeshObstacle>();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? swingAngle : 0f;

            if (isOpen)
                obstacle.enabled = false; // Allow pathing through when open
        }

        // Smoothly rotate toward target angle
        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);

        // Re-enable obstacle only once fully closed
        if (!isOpen && Mathf.Abs(Mathf.DeltaAngle(newY, 0f)) < 0.5f)
            obstacle.enabled = true;
    }
}