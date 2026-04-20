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

    [Header("Game Over Hud")]
    [SerializeField] private GameObject gameover_panel;
    [SerializeField] private TMP_Text gameover_text;

    [Header("Game Freezing Necessities")]
    [SerializeField] private PlayerAiming player;
    [SerializeField] private InvPistol gun;
    [SerializeField] private GameObject crosshair;

    private bool is_final_win = false;

    // used to change the text of the checklist
    private TMP_Text checklistText;


    public void MainMenu()
    {
        Time.timeScale = 1f;
        SafehouseState.Reset();
        EventBus.Clear();
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

        //EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
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
    }

    private bool is_paused = false;
    private bool can_pause = true;

    private void Update()
    {
        //if (gameover_panel.activeSelf && Input.GetKeyDown(KeyCode.F))
        //{
        //    Time.timeScale = 1;
        //    if (is_final_win)
        //    {
        //        SafehouseState.Reset();
        //        SceneManager.LoadScene("Main Menu");
        //    }
        //    else
        //    {
        //        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //    }
        //}

        if (Input.GetKeyDown(KeyCode.Escape) && can_pause && !MapController.is_open && !BuyMenuController.IsOpen && !Whiteboard.IsOpen)
            ShowHideEscapeMenu();
    }

    public void ShowHideEscapeMenu()
    {
        if (is_paused)
        {
            EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
            escapePanel.SetActive(false);
        }
        else
        {
            EventBus.Publish(new GameFreezeEvent() { freeze_map = true });
            escapePanel.SetActive(true);
        }

        is_paused = !is_paused;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
        EventBus.Subscribe<WinEvent>(OnWinEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<WinEvent>(OnWinEvent);
    }


    private void OnGameOver(GameOverEvent e)
    {
        Time.timeScale = 0;
        //gameover_panel.SetActive(true);
        //gameover_text.text = "<b>Game Over! \n\n You Lose!</b>";
        //gameover_text.color = Color.red;
    }

    private void OnWinEvent(WinEvent e)
    {
        is_final_win = e.is_final_win;
        EventBus.Publish(new GameFreezeEvent() { freeze_map = true });
        can_pause = false;
        gameover_panel.SetActive(true);
        gameover_text.text = "<b>Game Over! \n\n You Win!</b>";
        gameover_text.color = Color.green;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}