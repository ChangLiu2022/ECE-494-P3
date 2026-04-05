using UnityEngine;
using static GunEvents;
using static GameEvents;

public class PistolPickup : MonoBehaviour
{
    [SerializeField] private float pickup_range = 2f;
    [SerializeField] private float flash_range = 5f;
    [SerializeField] private KeyCode pickup_key = KeyCode.E;
    [SerializeField] private Transform player;

    [Header("Flash Settings")]
    [SerializeField] private Color flash_color = Color.yellow;
    [SerializeField] private float flash_speed = 3f;

    private SpriteRenderer sprite_renderer;
    private Color original_color;

    private void Awake()
    {
        sprite_renderer = GetComponent<SpriteRenderer>();
        original_color = sprite_renderer.color;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (player == null)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= pickup_range && Input.GetKeyDown(pickup_key))
        {
            SafehouseState.gun_collected = true;
            EventBus.Publish(new WeaponChangedEvent("Pistol"));
            EventBus.Publish(new FirstHitEvent());
            Destroy(gameObject);
            return;
        }

        if (dist <= flash_range) PulseFlash();
        else ResetColor();
    }

    private void PulseFlash()
    {
        float t = (Mathf.Sin(Time.time * flash_speed) + 1f) / 2f;
        sprite_renderer.color = Color.Lerp(original_color, flash_color, t);
    }

    private void ResetColor()
    {
        sprite_renderer.color = original_color;
    }
}
