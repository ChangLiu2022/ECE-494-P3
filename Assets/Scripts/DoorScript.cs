using UnityEngine;

public class Door : MonoBehaviour
{
    public float interactRange = 2f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;

    private Transform player;
    private bool isOpen = false;
    private float targetAngle = 0f;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? swingAngle : 0f;
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }
}
using UnityEngine;

public class Door : MonoBehaviour
{
    public float interactRange = 2f;
    public float swingAngle = 90f;
    public float swingSpeed = 3f;

    private Transform player;
    private bool isOpen = false;
    private float targetAngle = 0f;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;
            targetAngle = isOpen ? swingAngle : 0f;
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }
}