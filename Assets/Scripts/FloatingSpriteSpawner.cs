using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FloatingSpriteSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Sprite sprite;

    [Header("Timing")]
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int spritesPerBurst = 2;

    [Header("Visuals")]
    [SerializeField] private Vector2 size = new Vector2(100f, 100f);

    private void Start()
    {
        InvokeRepeating(nameof(SpawnBurst), 0f, spawnInterval);
    }

    void SpawnBurst()
    {
        for (int i = 0; i < spritesPerBurst; i++)
        {
            SpawnSprite();
        }
    }

    void SpawnSprite()
    {
        Vector2 start = GetRandomOffscreenPoint();
        Vector2 end = GetRandomOffscreenPoint();

        GameObject obj = new GameObject("FloatingSprite", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvasRect, false);
        obj.transform.SetAsFirstSibling();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = start;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false; // UI optimization

        StartCoroutine(MoveAndDestroy(rect, img, start, end));
    }

    IEnumerator MoveAndDestroy(RectTransform rect, Image img, Vector2 start, Vector2 end)
    {
        float time = 0f;

        // Compute direction and rotation once
        Vector2 dir = (end - start).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0f, 0f, 270f - angle);

        while (time < moveDuration)
        {
            float t = time / moveDuration;

            // smooth movement (ease in/out)
            t = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition = Vector2.Lerp(start, end, t);

            time += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = end;

        Destroy(rect.gameObject);
    }

    Vector2 GetRandomOffscreenPoint()
    {
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        float offset = 100f;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: return new Vector2(Random.Range(-width / 2, width / 2), height / 2 + offset); // top
            case 1: return new Vector2(Random.Range(-width / 2, width / 2), -height / 2 - offset); // bottom
            case 2: return new Vector2(-width / 2 - offset, Random.Range(-height / 2, height / 2)); // left
            default: return new Vector2(width / 2 + offset, Random.Range(-height / 2, height / 2)); // right
        }
    }
}