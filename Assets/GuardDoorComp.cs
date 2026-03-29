using UnityEngine;

public class GuardDoorComp : MonoBehaviour
{
    /*
    [SerializeField] private GuardDoors door;

    private int guardsInTrigger = 0;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Guard hit the door!");
        if (other.CompareTag("EXIT"))
        {
            guardsInTrigger++;
            door.IsBlockedByGuard = guardsInTrigger > 0;
            if (door != null) door.OpenDoor(other.transform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Guard left the door!");
        if (other.CompareTag("EXIT"))
        {
            guardsInTrigger = Mathf.Max(guardsInTrigger - 1, 0);
            door.IsBlockedByGuard = guardsInTrigger > 0;
            if (guardsInTrigger <= 0)
            {
                if (door != null) door.CloseDoor();
            }
        }
    }
    */
}
