using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using static GameEvents;
using static GunEvents;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class HUDController : MonoBehaviour
{
    [Header("Panel Dependencies")]
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private TMP_Text ammoDisplay;

    [Header("Visual Settings")]
    [SerializeField] private float bounceDuration = 0.15f;

    // used to change the text of the checklist
    private TMP_Text checklistText;
    private bool goldCollected = false;

    // used to update bullet count
    private HasInventory inv;

    // used to prevent coroutine being called while it's already running
    private bool isFlashing = false;

    // saves the checklistText gameObject
    private void Awake()
    {
        if (checklistPanel == null)
        {
            Debug.LogError("ChecklistPanel not assigned!");
            return;
        }

        // automatically finds the first TMP_Text in the panel's children
        checklistText = checklistPanel.GetComponentInChildren<TMP_Text>();
        if (checklistText == null)
        {
            Debug.LogError("No TMP_Text found in checklistPanel children!");
        }

        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            inv = player.GetComponent<HasInventory>();

            if (inv == null)
            {
                Debug.LogError("Player found but HasInventory component is missing.");
            }
        }
        else
        {
            Debug.LogError("No GameObject with name 'Player' found in the scene.");
        }

        if (inv != null && ammoDisplay != null) UpdateAmmoCount(new AmmoChangedEvent());
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<AmmoChangedEvent>(UpdateAmmoCount);
        EventBus.Subscribe<FailedToFireEvent>(FlashIndicator);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<AmmoChangedEvent>(UpdateAmmoCount);
        EventBus.Unsubscribe<FailedToFireEvent>(FlashIndicator);
    }

    // crosses off the first objective if the gold is collected
    private void OnAlertEvent(AlertEvent e)
    {
        if (goldCollected || checklistText == null) return;

        goldCollected = true;

        checklistText.text =
            "<b><s>1) Collect the gold</s>\n" +
            "2) Exit the map</b>";
    }

    private void OnGameOver(GameOverEvent e)
    {
        if (!goldCollected || checklistText == null) return;

        checklistText.text =
            "<b><s>1) Collect the gold\n" +
            "2) Exit the map</s></b>";
    }

    // updates the ammo count
    private void UpdateAmmoCount(AmmoChangedEvent e)
    {
        if (inv == null) return;
        if (ammoDisplay == null)
        {
            Debug.Log("Ammo display not assigned!"); return;
        }

        ammoDisplay.text = "Ammo: " + inv.BulletCount.ToString();
    }

    // flashes the ammo count to indicate that the player is out of bullets
    private void FlashIndicator(FailedToFireEvent e)
    {
        if(!isFlashing)
        {
            isFlashing = true;
            StartCoroutine(FlashBounceRoutine());
        } else
        {
            Debug.Log("Flashing coroutine already running!");
        }
    }

    private IEnumerator FlashBounceRoutine()
    {
        if (ammoDisplay == null) yield break;

        RectTransform rect = ammoDisplay.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Color originalColor = ammoDisplay.color;
        Vector3 originalScale = rect.localScale;

        Color flashColor = Color.red;
        Vector3 bounceScale = originalScale * 1.2f;

        // grow + turn red
        ammoDisplay.color = flashColor;
        rect.localScale = bounceScale;

        yield return new WaitForSeconds(bounceDuration);

        // return to normal
        ammoDisplay.color = originalColor;
        rect.localScale = originalScale;

        isFlashing = false;
    }
}
