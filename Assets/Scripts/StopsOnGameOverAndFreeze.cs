using UnityEngine;
using static GameEvents;

[RequireComponent(typeof(AudioSource))]
public class StopsOnGameOverAndFreeze : MonoBehaviour
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
        EventBus.Subscribe<GameFreezeEvent>(OnGameFreeze);
        EventBus.Subscribe<GameUnfreezeEvent>(OnGameUnfreeze);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<GameFreezeEvent>(OnGameFreeze);
        EventBus.Unsubscribe<GameUnfreezeEvent>(OnGameUnfreeze);
    }

    private void OnGameOver(GameOverEvent e)
    {
        if (audioSource != null)
        {
            audioSource.Stop();          // stop current sound
            audioSource.enabled = false; // prevent future playback
        }
    }

    private void OnGameFreeze(GameFreezeEvent e)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause(); // pauses so it can resume later
        }
    }

    private void OnGameUnfreeze(GameUnfreezeEvent e)
    {
        if (audioSource != null && audioSource.enabled)
        {
            audioSource.UnPause(); // resumes from where it left off
        }
    }
}