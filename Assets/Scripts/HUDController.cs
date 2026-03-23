using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;
using static GunEvents;

public class HUDController : MonoBehaviour
{
    [Header("Panel Dependencies")]
    [SerializeField] private GameObject escapePanel;
    [SerializeField] private GameObject controlsPanel;
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
        UnfreezeGame();
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

        UpdateAmmoCount(new AmmoChangedEvent());
    }

    private bool is_paused = false;
    private bool can_pause = true;

    private void Update()
    {
        if (gameover_panel.activeSelf == true && Input.GetKeyDown(KeyCode.F))
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if(Input.GetKeyDown(KeyCode.G) && can_pause)
        {
            ShowHideEscapeMenu();
        }
    }

    public void ShowHideEscapeMenu()
    {
        if (is_paused)
        {
            UnfreezeGame();
            escapePanel.SetActive(false);
        }
        else
        {
            FreezeGame();
            escapePanel.SetActive(true);
        }

        is_paused = !is_paused;
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
        FreezeGame();
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

    // updates the ammo count
    private void UpdateAmmoCount(AmmoChangedEvent e)
    {
        if (inv == null) return;
        if (ammoContainer == null)
        {
            Debug.Log("Ammo container not assigned!"); return;
        }
        if(ammoPrefab == null)
        {
            Debug.Log("Ammo prefab not assigned!"); return;
        }

        foreach (var icon in ammoIcons)
            Destroy(icon);

        ammoIcons.Clear();

        for (int i = 0; i < inv.BulletCount; i++)
        {
            Debug.Log("Done!");
            GameObject icon = Instantiate(ammoPrefab, ammoContainer);
            ammoIcons.Add(icon);
        }
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

    private void FreezeGame()
    {
        Time.timeScale = 0f;
        crosshair.SetActive(false);
        Cursor.visible = true;
        if (gun != null) gun.enabled = false;
        if (player != null) player.enabled = false;
    }

    private void UnfreezeGame()
    {
        Time.timeScale = 1f;
        crosshair.SetActive(true);
        Cursor.visible = false;
        if (gun != null) gun.enabled = true;
        if (player != null) player.enabled = true;
    }

    public void ShowControlsPanel()
    {
        controlsPanel.SetActive(true);
        escapePanel.SetActive(false);
    }

    public void HideControlsPanel()
    {
        controlsPanel.SetActive(false);
        escapePanel.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
