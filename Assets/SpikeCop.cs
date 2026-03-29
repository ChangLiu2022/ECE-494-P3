using UnityEngine;


public class SpikeCop : MonoBehaviour
{
    [Header("Spike")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private Transform spawnPoint;

    private bool _deployed;

    public void OnDetect(Collider other)
    {
        if (_deployed) return;
        if (!other.CompareTag("PlayerCar") && !other.transform.root.CompareTag("PlayerCar")) return;

        Deploy();
    }

    private void Deploy()
    {
        _deployed = true;

        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Quaternion rot = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

        Instantiate(spikePrefab, pos, rot);
    }
}