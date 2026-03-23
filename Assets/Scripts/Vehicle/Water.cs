using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private LayerMask vehicleLayer;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("PlayerCar") && !other.transform.root.CompareTag("PlayerCar")) return;

        EventBus.Publish(new GameEvents.GameOverEvent());
    }

    private void OnTriggerStay(Collider other)
    {

        if (!other.CompareTag("PlayerCar") && !other.transform.root.CompareTag("PlayerCar")) return;

        EventBus.Publish(new GameEvents.GameOverEvent());
    }
}
