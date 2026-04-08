using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEvents; // assuming your events are in GameEvents

public class PlaysSounds : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip noiseWaveClip; // Assign in Inspector
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;

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
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<NoiseWaveEvent>(OnNoiseWave);
    }

    private void OnNoiseWave(NoiseWaveEvent e)
    {
        // Play the assigned clip once
        if (noiseWaveClip != null && e.is_gunshot)
        {
            audioSource.PlayOneShot(noiseWaveClip, volume);
        }
    }
}