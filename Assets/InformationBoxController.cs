using System.Collections;
using UnityEngine;
using TMPro;

public class InformationBoxController : MonoBehaviour
{
    public static InformationBoxController instance;

    [SerializeField] private float display_duration = 5f;
    [SerializeField] private float fade_duration = 1f;
    [SerializeField] private float blink_duration = 0.08f;
    [SerializeField] private int blink_count = 2;

    private CanvasGroup canvas_group;
    private TMP_Text panel_text;
    private Coroutine active_coroutine;

    private void Awake()
    {
        instance = this;

        canvas_group = GetComponent<CanvasGroup>();
        if (canvas_group == null)
            canvas_group = gameObject.AddComponent<CanvasGroup>();

        panel_text = GetComponentInChildren<TMP_Text>();
        gameObject.SetActive(false);
    }

    public void Show(string message, float duration = -1f)
    {
        float use_duration = duration > 0f ? duration : display_duration;
        bool was_active = gameObject.activeSelf;

        if (active_coroutine != null)
            StopCoroutine(active_coroutine);

        gameObject.SetActive(true);
        active_coroutine = StartCoroutine(ShowRoutine(message, use_duration, was_active));
    }

    private IEnumerator ShowRoutine(string message, float duration, bool was_active)
    {
        canvas_group.alpha = 1f;

        if (panel_text != null)
            panel_text.text = message;

        if (was_active)
        {
            for (int i = 0; i < blink_count; i++)
            {
                canvas_group.alpha = 0f;
                yield return new WaitForSeconds(blink_duration);
                canvas_group.alpha = 1f;
                yield return new WaitForSeconds(blink_duration);
            }
        }

        yield return new WaitForSeconds(duration);

        float elapsed = 0f;
        while (elapsed < fade_duration)
        {
            elapsed += Time.deltaTime;
            canvas_group.alpha = Mathf.Lerp(1f, 0f, elapsed / fade_duration);
            yield return null;
        }

        gameObject.SetActive(false);
        active_coroutine = null;
    }
}
