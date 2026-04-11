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
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWave);
        EventBus.Unsubscribe<AlertEvent>(OnAlarm);
        EventBus.Unsubscribe<PowerOffEvent>(OnLightsOff);
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
}