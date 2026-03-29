using UnityEngine;
using static GameEvents;


// manager that sits somewhere in the scene
// listens for NoiseWaveEvents and spawns
// noise wave objects that expand and fade out
public class NoiseWaveManager : MonoBehaviour
{
    [SerializeField] private GameObject wave_prefab;


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
        GameObject wave = 
            Instantiate(wave_prefab, e.origin, Quaternion.identity);

        wave.GetComponent<NoiseWave>().Initialize(e.origin, e.radius);
    }
}
