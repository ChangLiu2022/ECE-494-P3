using UnityEngine;

public class SpikeStrip : MonoBehaviour
{
    [SerializeField] private float pitForce = 5f;
    [SerializeField] private int coinLoss = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerCar") && !other.transform.root.CompareTag("PlayerCar")) return;

        if (pit != null)
            pit.Pit(pitForce, coinLoss);

    }
}