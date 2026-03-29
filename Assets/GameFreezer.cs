using UnityEngine;
using static GameEvents;

public class GameFreezer : MonoBehaviour
{
    [Header("References to Freeze")]
    [SerializeField] private PlayerAiming player;
    [SerializeField] private GameObject weapon_pivot;
    [SerializeField] private GameObject crosshair;

    private void OnEnable()
    {
        EventBus.Subscribe<GameFreezeEvent>(OnFreezeEvent);
        EventBus.Subscribe<GameUnfreezeEvent>(OnUnfreezeEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameFreezeEvent>(OnFreezeEvent);
        EventBus.Unsubscribe<GameUnfreezeEvent>(OnUnfreezeEvent);
    }

    private void OnFreezeEvent(GameFreezeEvent e)
    {
        Time.timeScale = 0f;
        if (crosshair != null) crosshair.SetActive(false);
        Cursor.visible = true;
        if (weapon_pivot != null) weapon_pivot.SetActive(false);
        if (player != null) player.enabled = false;
    }

    private void OnUnfreezeEvent(GameUnfreezeEvent e)
    {
        Time.timeScale = 1f;
        if (crosshair != null) crosshair.SetActive(true);
        Cursor.visible = false;
        if (weapon_pivot != null) weapon_pivot.SetActive(true);
        if (player != null) player.enabled = true;
    }
}