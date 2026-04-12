using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class PlaySoundOnEnter : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip sound;

    [Header("Mode")]
    [SerializeField] private bool continuousLoop = false; // A = true, B = false

    [Header("Random Interval (Mode B only)")]
    [SerializeField] private float minMultiplier = 1.25f;
    [SerializeField] private float maxMultiplier = 4f;

    [Header("Disable When Object Is Destroyed")]
    [SerializeField] private GameObject trackedObject;

    private AudioSource audioSource;
    private Coroutine routine;
    private bool playerInside;
    private bool hasTrackedObject;
    private bool wasActive = true;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        GetComponent<Collider>().isTrigger = true;
        
        hasTrackedObject = trackedObject != null;
    }

    private void Update()
    {
        if (!hasTrackedObject) return;

        bool isActive = trackedObject != null && trackedObject.activeInHierarchy;

        if (!isActive && wasActive) HandleDisabled();

        if (isActive && !wasActive) HandleReenabled();

        wasActive = isActive;
    }

    private void HandleDisabled()
    {
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        audioSource.Stop();
        playerInside = false;
    }

    private void HandleReenabled()
    {
        if (TryGetComponent<Collider>(out var col)) col.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (sound == null) return;

        playerInside = true;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(MainRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        audioSource.Stop();
    }

    private IEnumerator MainRoutine()
    {
        if (continuousLoop)
        {
            // MODE A: continuous hum
            audioSource.clip = sound;
            audioSource.loop = true;
            audioSource.Play();

            while (playerInside)
                yield return null;

            audioSource.Stop();
        }
        else
        {
            // MODE B: random intermittent sounds
            while (playerInside)
            {
                float waitTime = Random.Range(
                    sound.length * minMultiplier,
                    sound.length * maxMultiplier
                );

                yield return new WaitForSeconds(waitTime);

                if (!playerInside) yield break;

                audioSource.PlayOneShot(sound);
            }
        }

        routine = null;
    }
}