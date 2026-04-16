using UnityEngine;
using static GameEvents;


// manager that sits somewhere in the scene
// listens for NoiseWaveEvents and spawns
// noise wave objects that expand and fade out
public class NoiseWaveManager : MonoBehaviour
{
    [SerializeField] private float spawnCooldown = 0.3f;
    [SerializeField] private GameObject wave_prefab;

    private float lastSpawnTime = -Mathf.Infinity;


    private void OnEnable()
    {
        EventBus.Subscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWaveEvent);
    }


    private void OnNoiseWaveEvent(NoiseWaveEvent e)
    {
        if (Time.time - lastSpawnTime < spawnCooldown) return;
        lastSpawnTime = Time.time;

        GameObject wave =
            Instantiate(wave_prefab, e.origin, Quaternion.identity);

        wave.GetComponent<NoiseWave>().Initialize(e.origin, e.radius);
    }
}
