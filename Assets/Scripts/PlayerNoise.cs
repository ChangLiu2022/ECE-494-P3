using UnityEngine;
using static GameEvents;

public class PlayerNoise : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;

    [Header("Sprint Noise")]
    [Tooltip("Speed at which the player is considered sprinting.")]
    [SerializeField] private float sprint_threshold = 6f;
    [SerializeField] private float sprint_noise_radius = 5f;
    [Tooltip("How often a wave is emitted while sprinting.")]
    [SerializeField] private float wave_interval = 0.5f;

    private float wave_timer = 0f;


    private void Update()
    {
        // if the player is not moving fast enough
        if (movement.Velocity.magnitude < sprint_threshold)
        {
            // stop producing waves
            wave_timer = 0f;
            return;
        }

        wave_timer += Time.deltaTime;

        // if the timer has reached the interval, emit a wave of noise
        if (wave_timer >= wave_interval)
        {
            wave_timer = 0f;

            EventBus.Publish(new NoiseWaveEvent
            {
                origin = transform.position,
                radius = sprint_noise_radius
            });
        }
    }
}
