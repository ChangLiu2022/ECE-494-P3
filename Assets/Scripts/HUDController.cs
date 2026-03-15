using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameEvents;

public class HUDController : MonoBehaviour
{
    [Header("Panel Dependencies")]
    [SerializeField] private GameObject checklistPanel;

    // used to change the text of the checklist
    private TMP_Text checklistText;
    private bool goldCollected = false;

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
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
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
}
