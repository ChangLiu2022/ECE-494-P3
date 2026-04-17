using UnityEngine;


public class OneWayDoors : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        EventBus.Publish(new GameEvents.PlayerEnteredMapEvent());
    }
}