using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;
using static GunEvents;

public class HUDController : MonoBehaviour
{
    public static HUDController instance;

    [Header("Panel Dependencies")]
    [SerializeField] private GameObject escapePanel;
    //[SerializeField] private TMP_Text ammoDisplay;
    [SerializeField] private Transform ammoContainer;
    [SerializeField] private GameObject ammoPrefab;

    private List<GameObject> ammoIcons = new List<GameObject>();

    [Header("Visual Settings")]
    [SerializeField] private float bounceDuration = 0.15f;

    [Header("Game Over Hud")]
    [SerializeField] private GameObject gameover_panel;
    [SerializeField] private TMP_Text gameover_text;

    [Header("Game Freezing Necessities")]
    [SerializeField] private PlayerAiming player;
    [SerializeField] private InvPistol gun;
    [SerializeField] private GameObject crosshair;

    private static bool show_start_screen = true;
    private bool is_final_win = false;

    // used to change the text of the checklist
    private TMP_Text checklistText;
    private bool goldCollected = false;

    // used to update bullet count
    private HasInventory inv;

    // used to prevent coroutine being called while it's already running
    private bool isFlashing = false;


    public void MainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }


    public void ForceCloseEscape()
    {
        if (!is_paused) return;
        escapePanel.SetActive(false);
        is_paused = false;
    }


    public bool IsEscapeOpen => is_paused;


    // saves the checklistText gameObject
    private void Start()
    {
        instance = this;

        EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
        if (gameover_panel != null)
            gameover_panel.SetActive(false);

        if (escapePanel == null)
        {
            Debug.LogError("EscapePanel not assigned!");
            return;
        }

        // automatically finds the first TMP_Text in the panel's children
        checklistText = escapePanel.GetComponentInChildren<TMP_Text>();
        if (checklistText == null)
        {
            Debug.LogError("No TMP_Text found in EscapePanel children!");
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
    }

    private bool is_paused = false;
    private bool can_pause = true;

    private void Update()
    {
        if (gameover_panel.activeSelf && Input.GetKeyDown(KeyCode.F))
        {
            Time.timeScale = 1;
            if (is_final_win)
            {
                SafehouseState.Reset();
                SceneManager.LoadScene("Main Menu");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && can_pause && !MapController.is_open)
            ShowHideEscapeMenu();
    }

    public void ShowHideEscapeMenu()
    {
        if (is_paused)
        {
            EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
            escapePanel.SetActive(false);
            Debug.Log($"ESCAPE CLOSED: timeScale after = {Time.timeScale}");
        }
        else
        {
            EventBus.Publish(new GameFreezeEvent() { freeze_map = true });
            escapePanel.SetActive(true);
            Debug.Log($"ESCAPE OPEN: timeScale after = {Time.timeScale}");
        }

        is_paused = !is_paused;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<FailedToFireEvent>(FlashIndicator);
        EventBus.Subscribe<WinEvent>(OnWinEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<FailedToFireEvent>(FlashIndicator);
        EventBus.Unsubscribe<WinEvent>(OnWinEvent);
    }

    // crosses off the first objective if the gold is collected
    private void OnAlertEvent(AlertEvent e)
    {
        if (goldCollected || checklistText == null) return;

        goldCollected = true;

        checklistText.text =
            "<b>Objectives:\n" +
            "<s>1) Collect the gold</s>\n" +
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
        is_final_win = e.is_final_win;
        EventBus.Publish(new GameFreezeEvent() { freeze_map = true });
        can_pause = false;
        gameover_panel.SetActive(true);
        gameover_text.text = "<b>Game Over! \n\n You Win!</b>";
        gameover_text.color = Color.green;

        if (!goldCollected || checklistText == null) return;

        checklistText.text =
            "<b>Objectives:" +
            "<s>1) Collect the gold\n" +
            "2) Exit the map</s></b>";
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
        // Spawn temporary ammo icon
        GameObject tempIcon = Instantiate(ammoPrefab, ammoContainer);

        RectTransform rect = tempIcon.GetComponent<RectTransform>();
        UnityEngine.UI.Image img = tempIcon.GetComponent<UnityEngine.UI.Image>();

        if (rect == null || img == null)
        {
            Destroy(tempIcon);
            yield break;
        }

        Vector3 originalScale = rect.localScale;

        Color flashColor = Color.red;
        Vector3 bounceScale = originalScale * 1.2f;

        // Flash + grow
        img.color = flashColor;
        rect.localScale = bounceScale;

        yield return new WaitForSeconds(bounceDuration);

        // Return to normal
        rect.localScale = originalScale;

        yield return new WaitForSeconds(0.05f); // tiny delay for readability

        // Remove the icon
        Destroy(tempIcon);

        isFlashing = false;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}