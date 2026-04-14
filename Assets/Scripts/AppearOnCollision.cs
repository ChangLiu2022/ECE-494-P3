using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using static GameEvents;
using static GunEvents;

public class AppearOnCollision : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.3f;

    private TMP_Text text;
    private Coroutine currentFade;
    private bool shouldAppear = true;

    [SerializeField] private bool is_map = false;
    [SerializeField] private bool is_gun = false;

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
    }

    private void OnAlertEvent(AlertEvent e)
    {
        //shouldAppear = false;
    }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (text == null) Debug.Log("TMP_Text component not found!");

        SetAlpha(0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Body") && shouldAppear)
        {
            if (is_map && SafehouseState.paper_collected) return;
            if (is_gun && SafehouseState.gun_collected) return;
            StartFade(1f); // fade in
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Body") && shouldAppear)
        {
            StartFade(0f); // fade out
            shouldAppear = false;
        }
    }

    void StartFade(float targetAlpha)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeText(targetAlpha));
    }

    void SetAlpha(float a)
    {
        if (text == null) return;

        Color c = text.color;
        c.a = a;
        text.color = c;
    }

    IEnumerator FadeText(float targetAlpha)
    {
        float startAlpha = text.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }
}
