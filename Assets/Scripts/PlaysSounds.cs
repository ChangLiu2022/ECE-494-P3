using System.Collections;
using UnityEngine;
using static GameEvents; // assuming your events are in GameEvents

public class PlaysSounds : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip noiseWaveClip; // Assign in Inspector
    [SerializeField] private AudioClip alarmClip; // Assign in Inspector
    [SerializeField] private AudioClip guardHitClip; // Assign in Inspector
    [SerializeField] private AudioClip playerSpottedClip; // Assign in Inspector
    [SerializeField] private AudioClip upgradeClip; // Assign in Inspector
    [SerializeField] private AudioClip downgradeClip; // Assign in Inspector
    [SerializeField] private AudioClip barkClip; // Assign in Inspector
    [SerializeField] private AudioClip whineClip;
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;
    private bool isPlayingAlarm = false;

    private bool isStopped = false;

    void Awake()
    {
        // Add or get an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        // Subscribe to the custom event
        EventBus.Subscribe<NoiseWaveEvent>(OnNoiseWave);
        EventBus.Subscribe<AlertEvent>(OnAlarm);
        EventBus.Subscribe<PowerOffEvent>(OnLightsOff);
        EventBus.Subscribe<GuardShotEvent>(OnGuardShot); 
        EventBus.Subscribe<PlayerSpottedEvent>(OnPlayerSpotted); 
        EventBus.Subscribe<GuardShootsEvent>(GuardShoots); 
        EventBus.Subscribe<UpgradeActivatedEvent>(UpgradeActivated); 
        EventBus.Subscribe<DowngradeActivatedEvent>(DowngradeActivated); 
        EventBus.Subscribe<DogGrabbed>(OnDogGrabbed);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWave);
        EventBus.Unsubscribe<AlertEvent>(OnAlarm);
        EventBus.Unsubscribe<PowerOffEvent>(OnLightsOff);
        EventBus.Unsubscribe<GuardShotEvent>(OnGuardShot); 
        EventBus.Unsubscribe<PlayerSpottedEvent>(OnPlayerSpotted); 
        EventBus.Unsubscribe<GuardShootsEvent>(GuardShoots); 
        EventBus.Unsubscribe<UpgradeActivatedEvent>(UpgradeActivated); 
        EventBus.Unsubscribe<DowngradeActivatedEvent>(DowngradeActivated);
        EventBus.Unsubscribe<DogGrabbed>(OnDogGrabbed);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent e)
    {
        isStopped = true;
        if (whineClip != null)
        {
            audioSource.PlayOneShot(whineClip, volume);
        }
    }

    private void OnDogGrabbed(DogGrabbed e)
    {
        if (barkClip != null && !isStopped)
        {
            audioSource.PlayOneShot(barkClip, volume);
        }
    }

    private void GuardShoots(GuardShootsEvent e)
    {
        if (noiseWaveClip != null && !isStopped)
        {
            audioSource.PlayOneShot(noiseWaveClip, volume);
        }
    }

    private void OnGuardShot(GuardShotEvent e)
    {
        if (e.not_guard && !isStopped) 
            return;

        if (guardHitClip != null)
        {
            audioSource.PlayOneShot(guardHitClip, volume*2f); // Play guard hit sound at double volume for emphasis
        }
    }

    private void OnNoiseWave(NoiseWaveEvent e)
    {
        // Play the assigned clip once
        if (noiseWaveClip != null && e.is_gunshot && !isStopped)
        {
            audioSource.PlayOneShot(noiseWaveClip, volume);
        }
    }

    private void OnAlarm(AlertEvent e)
    {
        // Play the assigned clip once
        if (alarmClip != null && !isPlayingAlarm && !isStopped)
        {
            isPlayingAlarm = true;
            audioSource.PlayOneShot(alarmClip, volume);
            StartCoroutine(WaitUntilNotPlaying(audioSource));
        }
    }

    private void OnLightsOff(PowerOffEvent e)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    IEnumerator WaitUntilNotPlaying(AudioSource source)
    {
        while (source.isPlaying)
        {
            yield return null; // wait for the next frame
        }
        isPlayingAlarm = false;
    }

    private void OnPlayerSpotted(PlayerSpottedEvent e)
    {
        if (playerSpottedClip != null && !isStopped)
        {
            audioSource.PlayOneShot(playerSpottedClip, volume);
        }
    }

    private void UpgradeActivated(UpgradeActivatedEvent e)
    {
        if (upgradeClip != null && !isStopped) audioSource.PlayOneShot(upgradeClip, volume*2.5f);
    }

    private void DowngradeActivated(DowngradeActivatedEvent e)
    {
        if (downgradeClip != null && !isStopped) audioSource.PlayOneShot(downgradeClip, volume*2.5f);
    }
}