using UnityEngine;

public class ChaseController : MonoBehaviour
{
    [Header("Heat")]
    [SerializeField][Range(0f, 1f)] private float heat = 0.5f;

    [Header("Spike Cops")]
    [SerializeField] private GameObject spikeCopPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void OnEnable()
    {
        EventBus.Subscribe<GameEvents.GoldEvent>(OnGold);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameEvents.GoldEvent>(OnGold);
    }

    private void OnGold(GameEvents.GoldEvent evt)
    {
        SpawnSpikeCops();
    }

    private void SpawnSpikeCops()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (spikeCopPrefab == null) return;

        int totalPoints = spawnPoints.Length;
        int toSpawn = Mathf.CeilToInt(totalPoints * heat);

        // Shuffle spawn points to pick random ones
        Transform[] shuffled = (Transform[])spawnPoints.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < toSpawn; i++)
        {
            Transform point = shuffled[i];
            Instantiate(spikeCopPrefab, point.position, point.rotation);
        }
    }
}