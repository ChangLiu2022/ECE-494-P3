using UnityEngine;
using static GameEvents;

public class GameFreezer : MonoBehaviour
{
    private static bool isFrozen = false;
    public static bool IsFrozen => isFrozen;

    [Header("References to Freeze")]
    [SerializeField] private PlayerAiming player_aiming;
    [SerializeField] private GameObject weapon_pivot;
    [SerializeField] private GameObject crosshair_canvas;
    [SerializeField] private GameObject map;

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
        isFrozen = true;
        Time.timeScale = 0f;
        if (crosshair_canvas != null) crosshair_canvas.SetActive(false);
        Cursor.visible = true;
        if (weapon_pivot != null) weapon_pivot.SetActive(false);
        if (player_aiming != null) player_aiming.enabled = false;
        if(e.freeze_map && map != null) map.SetActive(false);
    }

    private void OnUnfreezeEvent(GameUnfreezeEvent e)
    {
        isFrozen = false;
        Time.timeScale = 1f;
        if (crosshair_canvas != null) crosshair_canvas.SetActive(true);
        Cursor.visible = false;
        if (weapon_pivot != null) weapon_pivot.SetActive(true);
        if (player_aiming != null) player_aiming.enabled = true;
        if(e.freeze_map && map != null) map.SetActive(true);
    }
}