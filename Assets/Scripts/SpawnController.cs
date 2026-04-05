using UnityEngine;

public class SpawnController : MonoBehaviour
{
    [SerializeField] private bool activate = true;
    [SerializeField] private GameObject[] targets;
    [SerializeField] private bool triggerOnce = true;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player") && !other.CompareTag("PlayerCar")) return;


        foreach (var target in targets)
        {
            if (target != null)
                target.SetActive(activate);
        }

        if (triggerOnce)
        {
            Destroy(gameObject);
        }
    }
}
