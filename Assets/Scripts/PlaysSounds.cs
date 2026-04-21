using System.Collections;
using System.Diagnostics.Tracing;
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
    [SerializeField] private AudioClip guard_death;
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;
    private AudioSource alarmSource;
    private bool isPlayingAlarm = false;

    private bool isStopped = false;

    private float _lastHitSoundTime = -999f;
    [SerializeField] private float hitSoundCooldown = 0.3f; // min seconds between hit sounds

    void Awake()
    {
        // Add or get an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        alarmSource = gameObject.AddComponent<AudioSource>();
        alarmSource.loop = true;
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
        EventBus.Subscribe<GuardDead>(OnGuardDeath);
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
        EventBus.Unsubscribe<GuardDead>(OnGuardDeath);
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
        if (isStopped || e.not_guard) return;
        // throttle rapid hits regardless of source
        if (Time.time - _lastHitSoundTime < hitSoundCooldown) return;

        if (guardHitClip != null)
        {
            _lastHitSoundTime = Time.time;
            audioSource.PlayOneShot(guardHitClip, volume * 3f);
        }
    }

    private void OnGuardDeath(GuardDead e)
    {
        if (guard_death != null && !isStopped)
        {
            audioSource.PlayOneShot(guard_death, volume*2f); // Play guard death sound at double volume for emphasis
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

    //private void OnAlarm(AlertEvent e)
    //{
    //    // Play the assigned clip once
    //    if (alarmClip != null && !isPlayingAlarm && !isStopped)
    //    {
    //        isPlayingAlarm = true;
    //        alarmSource.PlayOneShot(alarmClip, volume);
    //        StartCoroutine(WaitUntilNotPlaying(alarmSource));
    //    }
    //}

    private void OnAlarm(AlertEvent e)
    {
        if (alarmClip != null && !alarmSource.isPlaying && !isStopped)
        {
            alarmSource.clip = alarmClip;
            alarmSource.volume = volume;
            alarmSource.loop = true;
            alarmSource.Play();
        }
    }

    private void OnLightsOff(PowerOffEvent e)
    {
        if (alarmSource.isPlaying)
        {
            alarmSource.Stop();
        }
    }

    //IEnumerator WaitUntilNotPlaying(AudioSource source)
    //{
    //    while (source.isPlaying)
    //    {
    //        yield return null; // wait for the next frame
    //    }
    //    isPlayingAlarm = false;
    //}

    private void OnPlayerSpotted(PlayerSpottedEvent e)
    {
        if (playerSpottedClip != null && !isStopped)
        {
            audioSource.PlayOneShot(playerSpottedClip, volume*2f);
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