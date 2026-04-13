using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using static GameEvents;

public class FadeManager : MonoBehaviour
{
    [Header("Background Music Manager")]
    [SerializeField]AudioSource bgMusicSource;

    [Header("Transition Sound Effects")]
    [SerializeField] private AudioClip runningClip;
    [SerializeField] private AudioClip engineClip;
    [SerializeField] private AudioClip doorClip;
    [SerializeField] private AudioClip tiresScreechingClip;

    private static FadeManager instance;
    public static FadeManager Instance => instance;


    private bool isLoading = false;
    private Image img;
    private AudioSource audioSource;
    private GameObject dog;
    private TMP_Text loadText;
    private float newVolume;

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        img = GetComponentInChildren<Image>(true);
        if (img == null) Debug.LogError("No Image found under FadeManager!");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogError("No AudioSource found on FadeManager!");

        Transform canvas = transform.Find("Canvas");
        Transform dogTransform = canvas != null ? canvas.Find("Dog") : null;
        if (dogTransform != null) 
        {
            dog = dogTransform.gameObject;
            dog.GetComponent<Image>().enabled = false;
        }
        else Debug.LogWarning("Dog child not found under Canvas!");

        loadText = canvas != null ? canvas.GetComponentInChildren<TMP_Text>() : null;
        if(loadText == null) Debug.LogWarning("No TMP_Text found under Canvas!");
        else loadText.enabled = false;
    }

    public void StartTransition(string sceneName, AudioSource musicSource = null, float fadeDuration = 1f)
    {
        if (!isLoading)
        {
            isLoading = true;
            if(musicSource == null) musicSource = bgMusicSource;
            StartCoroutine(Transition(sceneName, musicSource, fadeDuration));
        }
    }

    public IEnumerator FadeToBlack(AudioSource musicSource = null, float fadeDuration = 1f)
    {
        if (img == null) yield break;

        if(musicSource == null) musicSource = bgMusicSource;

        EventBus.Publish(new GameFreezeEvent() { freeze_map = true }); // Freeze game during fade out

        img.raycastTarget = true; // Block input during fade

        Color c = img.color;

        // Instant case
        if (fadeDuration <= 0f)
        {
            c.a = 1f;
            img.color = c;

            if (musicSource != null)
                musicSource.volume = 0f;

            yield break;
        }

        float startAlpha = c.a;
        float timer = 0f;
        float initialVolume = musicSource != null ? musicSource.volume : 1f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            // Smoothstep easing
            t = t * t * (3f - 2f * t);

            c.a = Mathf.Lerp(startAlpha, 1f, t);
            img.color = c;

            if (musicSource != null)
                musicSource.volume = Mathf.Lerp(initialVolume, 0f, t);

            yield return null;
        }

        // Ensure final state
        c.a = 1f;
        img.color = c;

        if (musicSource != null) musicSource.volume = 0f;
        if (dog != null && fadeDuration != 1.95f) dog.GetComponent<Image>().enabled = true;
        if(loadText != null && fadeDuration != 1.95f) loadText.enabled = true;
    }

    private IEnumerator PlayTransitionSounds(string sceneName = "")
{
    if (audioSource == null) yield break;

    // 1. Running
    if (runningClip != null)
    {
        if(sceneName == "Safehouse") audioSource.pitch = 1.65f;
        audioSource.PlayOneShot(runningClip);
        yield return new WaitForSecondsRealtime(runningClip.length/audioSource.pitch);
        audioSource.pitch = 1f;
    }

    // 2. Engine start
    if (engineClip != null)
    {
        if(sceneName == "Safehouse") audioSource.pitch = 1.5f;
        audioSource.PlayOneShot(engineClip);
        yield return new WaitForSecondsRealtime(engineClip.length/audioSource.pitch);
        audioSource.pitch = 1f;
    }

    // 3.5. Tires screeching
    if (tiresScreechingClip != null && sceneName == "Safehouse")
    {
        audioSource.PlayOneShot(tiresScreechingClip);
        yield return new WaitForSecondsRealtime(tiresScreechingClip.length);
    }

    // 3. Door slam
    if (doorClip != null)
    {
        audioSource.PlayOneShot(doorClip);
        yield return new WaitForSecondsRealtime(doorClip.length);
    }
}

    public IEnumerator FadeFromBlack(AudioSource musicSource = null, float fadeDuration = 1f)
    {
        if (img == null) yield break;

        if (dog != null) dog.GetComponent<Image>().enabled = false;
        if (loadText != null) loadText.enabled = false;

        EventBus.Publish(new GameFreezeEvent() { freeze_map = true });

        if (musicSource == null) musicSource = bgMusicSource;

        Color c = img.color;

        float startAlpha = c.a;
        float timer = 0f;

        float startVolume = musicSource != null ? musicSource.volume : 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);

            // Smoothstep easing
            t = t * t * (3f - 2f * t);

            // Screen fade
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            img.color = c;
            
            if (musicSource != null) musicSource.volume = Mathf.Lerp(startVolume, newVolume, t);

            yield return null;
        }

        // Final state
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;

        if (musicSource != null) musicSource.volume = newVolume;

        EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
    }

    private IEnumerator Transition(string sceneName, AudioSource musicSource, float fadeDuration)
    {
        newVolume = sceneName == "Safehouse" ? 0.2f : 0.5f;

        yield return StartCoroutine(FadeToBlack(musicSource, fadeDuration));

        if(audioSource != null && fadeDuration != 1.95f) yield return StartCoroutine(PlayTransitionSounds(sceneName));
        else yield return null;

        SceneManager.LoadScene(sceneName);

        yield return null;

        yield return StartCoroutine(FadeFromBlack(musicSource, fadeDuration / 2));

        isLoading = false;
    }
}