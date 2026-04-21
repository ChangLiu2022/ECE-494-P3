using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    [Header("Background Music Manager")]
    [SerializeField]AudioSource safehouseMusicSource;
    [SerializeField]AudioSource chillMusicSource;
    [SerializeField] AudioSource activeMusicSource;

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
    private float newVolume;
    private HUDController currentHUD; // reference to scene HUD

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
    }

    public void StartTransition(string sceneName, AudioSource musicSource = null, float fadeDuration = 1f, AudioSource secondMusicSource = null)
    {
        if (!isLoading)
        {
            isLoading = true;

            if (musicSource == null)
            {
                string current_scene = SceneManager.GetActiveScene().name;
                if (current_scene == "Safehouse") musicSource = safehouseMusicSource;
                else if (current_scene.StartsWith("CutScene")) musicSource = null;
                else musicSource = activeMusicSource.volume > 0f ? activeMusicSource : chillMusicSource;
            }

            StartCoroutine(Transition(sceneName, musicSource, fadeDuration, secondMusicSource));
        }
    }

    public IEnumerator FadeToBlack(AudioSource musicSource = null, float fadeDuration = 1f, AudioSource secondMusicSource = null)
    {
        if (img == null) yield break;

        // EventBus.Publish(new GameFreezeEvent() { freeze_map = true }); // Freeze game during fade out

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
        float secondInitialVolume = secondMusicSource != null ? secondMusicSource.volume : 1f;

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
            if (secondMusicSource != null)
                secondMusicSource.volume = Mathf.Lerp(secondInitialVolume, 0f, t);

            yield return null;
        }

        // Ensure final state
        c.a = 1f;
        img.color = c;

        if (musicSource != null) musicSource.volume = 0f;
        if (secondMusicSource != null) secondMusicSource.volume = 0f;
    }

    public IEnumerator FadeFromBlack(AudioSource musicSource = null, float fadeDuration = 1f)
    {
        if (img == null) yield break;

        AudioSource car_audio_source = null;
        GameObject player_body = GameObject.FindWithTag("Body");
        if (player_body != null) car_audio_source = player_body.GetComponent<AudioSource>();
        else Debug.Log("Player body not found for fade manager!");

        //EventBus.Publish(new GameFreezeEvent() { freeze_map = true });

        Color c = img.color;

        float startAlpha = c.a;
        float timer = 0f;

        float startVolume = musicSource != null ? musicSource.volume : 0f;
        float carStartVolume = car_audio_source != null ? car_audio_source.volume : 0f;

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
            if (car_audio_source != null) car_audio_source.volume = Mathf.Lerp(carStartVolume, newVolume, t);

            yield return null;
        }

        // Final state
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;

        if (musicSource != null) musicSource.volume = newVolume;
        if (car_audio_source != null) car_audio_source.volume = newVolume;

        //EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });
    }

    private IEnumerator Transition(string sceneName, AudioSource musicSource, float fadeDuration, AudioSource secondMusicSource)
    {
        if (sceneName == "Safehouse") newVolume = 0.1f;
        else if (sceneName.StartsWith("CutScene")) newVolume = 0f;
        else newVolume = 0.5f;

        yield return StartCoroutine(FadeToBlack(musicSource, fadeDuration, secondMusicSource));

        if (sceneName == "Safehouse") SafehouseState.paper_collected = false;
        SceneManager.LoadScene(sceneName);

        yield return null;

        if (sceneName == "Safehouse") musicSource = safehouseMusicSource;
        else if (sceneName.StartsWith("CutScene")) musicSource = null;
        else musicSource = chillMusicSource;

        yield return StartCoroutine(FadeFromBlack(musicSource, fadeDuration / 2));

        isLoading = false;
    }
}