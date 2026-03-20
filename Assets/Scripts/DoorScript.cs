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
            
            if(isOpen)
            {
                targetAngle = 0f;
            }
            else
            {
                Vector3 localPos = transform.InverseTransformPoint(player.position);
                if (localPos.x > 0)
                {
                    targetAngle = -swingAngle;

                }
                else
                {
                    targetAngle = swingAngle;

                }

            }
            
            isOpen = !isOpen;
            
        }

        float currentY = transform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * swingSpeed);
        transform.localEulerAngles = new Vector3(0, newY, 0);
    }
}