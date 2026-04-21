using System.Collections;
using TMPro;
using UnityEngine;
using static GameEvents;

public class AppearOnCollision : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.3f;

    private TMP_Text text;
    private Coroutine currentFade;

    [SerializeField] private bool is_map = false;
    [SerializeField] private bool is_gun = false;
    [SerializeField] private bool no_dog_dependence = false;
    [SerializeField] private bool gone_after_first_level = false;

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<PlayerDisabledEvent>(OnPlayerDisabled);
        EventBus.Subscribe<DogGrabbed>(OnDogGrabbed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<PlayerDisabledEvent>(OnPlayerDisabled);
        EventBus.Unsubscribe<DogGrabbed>(OnDogGrabbed);
    }

    private void OnDogGrabbed(DogGrabbed e)
    {
        if(!no_dog_dependence) Destroy(gameObject);
    }

    private void OnPlayerDisabled(PlayerDisabledEvent e)
    {
        StartFade(0f);
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

        if (gone_after_first_level && SafehouseState.completed_tutorial)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Body"))
        {
            if (is_map && SafehouseState.paper_collected_once) return;
            if (is_gun && SafehouseState.gun_collected) return;
            StartFade(1f); // fade in
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Body"))
        {
            StartFade(0f); // fade out
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
