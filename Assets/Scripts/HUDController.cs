using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;
using static GunEvents;

public class HUDController : MonoBehaviour
{
    [Header("Panel Dependencies")]
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private TMP_Text ammoDisplay;

    [Header("Visual Settings")]
    [SerializeField] private float bounceDuration = 0.15f;

    [Header("Game Over Hud")]
    [SerializeField] private GameObject gameover_panel;
    [SerializeField] private TMP_Text gameover_text;

    [Header("Start Screen/Controls Overlay")]
    [SerializeField] private GameObject controls_panel;
    [SerializeField] private float start_delay = 3f;

    private static bool show_start_screen = true;

    // used to change the text of the checklist
    private TMP_Text checklistText;
    private bool goldCollected = false;

    // used to update bullet count
    private HasInventory inv;

    // used to prevent coroutine being called while it's already running
    private bool isFlashing = false;

    // saves the checklistText gameObject
    private void Start()
    {
        Time.timeScale = 0;

        if (controls_panel != null && show_start_screen == false) 
            controls_panel.SetActive(true);

        if (gameover_panel != null)
            gameover_panel.SetActive(false);

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


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            if (show_start_screen == false)
            {
                controls_panel.SetActive(false);
            }

            else if (controls_panel != null)
            {
                controls_panel.SetActive(false);
                show_start_screen = false;
            }

            Time.timeScale = 1;
        }

        if (gameover_panel.activeSelf == true && Input.GetKeyDown(KeyCode.F))
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<AmmoChangedEvent>(UpdateAmmoCount);
        EventBus.Subscribe<FailedToFireEvent>(FlashIndicator);
        EventBus.Subscribe<WinEvent>(OnWinEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<AmmoChangedEvent>(UpdateAmmoCount);
        EventBus.Unsubscribe<FailedToFireEvent>(FlashIndicator);
        EventBus.Unsubscribe<WinEvent>(OnWinEvent);
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
        Time.timeScale = 0;
        gameover_panel.SetActive(true);
        gameover_text.text = "<b>Game Over! \n\n You Lose Bud!</b>";
        gameover_text.color = Color.red;
    }

    private void OnWinEvent(WinEvent e)
    {
        Time.timeScale = 0;
        gameover_panel.SetActive(true);
        gameover_text.text = "<b>Game Over! \n\n You Win!</b>";
        gameover_text.color = Color.green;

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

        ammoDisplay.text = "<b>Ammo: " + inv.BulletCount.ToString() + "</b>";
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
