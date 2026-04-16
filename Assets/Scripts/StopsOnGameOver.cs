using UnityEngine;
using static GameEvents;

[RequireComponent(typeof(AudioSource))]
public class StopsOnGameOver : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        // Automatically grab the AudioSource on this GameObject
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent e)
    {
        if (audioSource != null)
        {
            audioSource.Stop();     // stop current sound
            audioSource.enabled = false; // optional: prevent future playback
        }
    }
}