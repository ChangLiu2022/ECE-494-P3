using System.Collections;
using UnityEngine;
using TMPro;
using static GameEvents;

public class HeavyGuardTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("How long the timer lasts in seconds. Set per-scene.")]
    [SerializeField] private float timerDuration = 120f;

    [Header("UI")]
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private TMP_Text timerText;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private int flashCount = 3;

    private float timeRemaining;
    private bool timerRunning = false;
    private bool timerExpired = false;

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerEnteredMapEvent>(OnPlayerEnteredMap);
        EventBus.Subscribe<TimerExpiredEvent>(OnTimerExpired);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerEnteredMapEvent>(OnPlayerEnteredMap);
        EventBus.Unsubscribe<TimerExpiredEvent>(OnTimerExpired);
    }

    private void Start()
    {
        timeRemaining = timerDuration;

        // hide the timer panel until the player enters the map
        if (timerPanel != null)
            timerPanel.SetActive(false);

        UpdateTimerDisplay();
    }

    private void OnPlayerEnteredMap(PlayerEnteredMapEvent e)
    {
        if (timerRunning || timerExpired) return;

        // show the panel and start counting
        if (timerPanel != null)
            timerPanel.SetActive(true);

        timerRunning = true;
    }

    private void Update()
    {
        if (!timerRunning || timerExpired) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            timerExpired = true;
            UpdateTimerDisplay();
            StartCoroutine(FlashAndExpire());
            return;
        }

        UpdateTimerDisplay();
    }

    // called externally when laser is tripped
    public void ForceExpire()
    {
        if (timerExpired) return;

        // show the panel if it wasn't showing yet
        if (timerPanel != null)
            timerPanel.SetActive(true);

        timeRemaining = 0f;
        timerRunning = false;
        timerExpired = true;
        UpdateTimerDisplay();
        StartCoroutine(FlashAndExpire());
    }

    private IEnumerator FlashAndExpire()
    {
        Color originalColor = timerText.color;

        for (int i = 0; i < flashCount; i++)
        {
            timerText.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            timerText.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        timerText.color = Color.red;
        EventBus.Publish(new TimerExpiredEvent());
    }

    private void OnTimerExpired(TimerExpiredEvent e)
    {
        timerRunning = false;
        timerExpired = true;
        timeRemaining = 0f;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }
}
