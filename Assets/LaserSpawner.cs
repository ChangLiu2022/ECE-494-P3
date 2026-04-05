using UnityEngine;

public class LaserSpawner : MonoBehaviour
{
    [SerializeField] RectTransform canvasRect;
    [SerializeField] GameObject laserPrefab;

    [SerializeField] float duration = 0.3f;
    [SerializeField] float holdTime = 0.2f;

    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] int lasersPerBurst = 2;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnBurst), 0f, spawnInterval);
    }

    void SpawnBurst()
    {
        for (int i = 0; i < lasersPerBurst; i++)
        {
            SpawnLaser();
        }
    }

    void SpawnLaser()
    {
        Vector2 start = GetRandomOffscreenPoint();
        Vector2 end = GetRandomOffscreenPoint();

        GameObject obj = Instantiate(laserPrefab, canvasRect);
        obj.transform.SetAsFirstSibling();
        obj.GetComponent<Laser>().Init(start, end, duration, holdTime);
    }

    Vector2 GetRandomOffscreenPoint()
    {
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        float offset = 100f; // how far off-screen

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: return new Vector2(Random.Range(-width/2, width/2), height/2 + offset); // top
            case 1: return new Vector2(Random.Range(-width/2, width/2), -height/2 - offset); // bottom
            case 2: return new Vector2(-width/2 - offset, Random.Range(-height/2, height/2)); // left
            default: return new Vector2(width/2 + offset, Random.Range(-height/2, height/2)); // right
        }
    }
}