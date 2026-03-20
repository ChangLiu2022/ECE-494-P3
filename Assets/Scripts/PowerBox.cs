using UnityEngine;

using static GameEvents;


public class PowerBox : MonoBehaviour
{
    public float interactRange = 2f;
    private bool isPowered = true;
    private Transform player;
    
    public Transform switchPivot;
    public float upAngle = 25f;
    public float downAngle = -25f;

    
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        switchPivot.localEulerAngles = new Vector3(upAngle, 0, 0);
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isPowered)
            {
                EventBus.Publish(new PowerOffEvent());
                switchPivot.localEulerAngles = new Vector3(downAngle, 0, 0);
                
            }
            else
            {
                EventBus.Publish(new PowerOnEvent());
                switchPivot.localEulerAngles = new Vector3(upAngle, 0, 0);
            }
            isPowered = !isPowered;
            
        }
    }
}
