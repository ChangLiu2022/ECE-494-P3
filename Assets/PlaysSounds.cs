using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static GameEvents; // assuming your events are in GameEvents

public class PlaysSounds : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip noiseWaveClip; // Assign in Inspector
    [SerializeField] private AudioClip alarmClip; // Assign in Inspector
    [SerializeField] private AudioClip guardHitClip; // Assign in Inspector
    [SerializeField] private AudioClip playerSpottedClip; // Assign in Inspector
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;
    private bool isPlayingAlarm = false;

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
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWave);
        EventBus.Unsubscribe<AlertEvent>(OnAlarm);
        EventBus.Unsubscribe<PowerOffEvent>(OnLightsOff);
        EventBus.Unsubscribe<GuardShotEvent>(OnGuardShot); 
        EventBus.Unsubscribe<PlayerSpottedEvent>(OnPlayerSpotted); 
    }

    private void OnGuardShot(GuardShotEvent e)
    {
        if (guardHitClip != null)
        {
            audioSource.PlayOneShot(guardHitClip, volume*2f); // Play guard hit sound at double volume for emphasis
        }
    }

    private void OnNoiseWave(NoiseWaveEvent e)
    {
        // Play the assigned clip once
        if (noiseWaveClip != null && e.is_gunshot)
        {
            audioSource.PlayOneShot(noiseWaveClip, volume);
        }
    }

    private void OnAlarm(AlertEvent e)
    {
        // Play the assigned clip once
        if (alarmClip != null && !isPlayingAlarm)
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
        if (playerSpottedClip != null)
        {
            audioSource.PlayOneShot(playerSpottedClip, volume);
        }
    }
}