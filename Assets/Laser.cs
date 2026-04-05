using System.Collections;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public void Init(Vector2 start, Vector2 end, float duration, float holdTime)
    {
        StartCoroutine(Shoot(start, end, duration, holdTime));
    }

    private IEnumerator Shoot(Vector2 start, Vector2 end, float duration, float holdTime)
    {
        RectTransform rt = (RectTransform)transform;

        // Start at the start position
        rt.anchoredPosition = start;

        // Rotate to face the target
        Vector2 dir = end - start;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        float length = dir.magnitude/100f;
        rt.localScale = new Vector3(length, 1f, 1f);

        float timer = 0f;

        // Move from start → end
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            rt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        rt.anchoredPosition = end;

        // Hold at the end
        yield return new WaitForSeconds(holdTime);

        // Retract back to start
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            rt.anchoredPosition = Vector2.Lerp(end, start, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}