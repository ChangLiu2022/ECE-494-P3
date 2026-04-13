using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomlyMakesNoise : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip sound;

    [Header("Interval Multipliers")]
    [SerializeField] private float minIntervalMultiplier = 1.25f;
    [SerializeField] private float maxIntervalMultiplier = 4f;

    private AudioSource audioSource;
    private Coroutine loopRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (sound != null)
            loopRoutine = StartCoroutine(NoiseLoop());
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
            StopCoroutine(loopRoutine);
    }

    private IEnumerator NoiseLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(
                sound.length * minIntervalMultiplier,
                sound.length * maxIntervalMultiplier
            );

            yield return new WaitForSeconds(waitTime);

            audioSource.PlayOneShot(sound);
        }
    }
}