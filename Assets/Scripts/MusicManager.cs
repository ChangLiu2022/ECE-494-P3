using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip activeBGMusicSource; // Optional: assign in inspector for fallback
    [SerializeField] private AudioClip chillBGMusicSource; // Optional: assign in inspector for fallback

    private AudioSource audioSource;
    private string lastScene;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        // Start muted
        audioSource.volume = 0f;
    }

    void OnEnable()
    {
      EventBus.Subscribe<WinEvent>(e => StopBGMusic());  
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<WinEvent>(e => StopBGMusic());
    }

    void Start()
    {
        lastScene = SceneManager.GetActiveScene().name;
        ApplyMusic();
    }

    void Update()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != lastScene)
        {
            lastScene = currentScene;
            ApplyMusic();
        }
    }

    private void ApplyMusic()
    {
        if (audioSource == null) return;

        AudioClip targetClip =
            SceneManager.GetActiveScene().name == "Safehouse"
            ? chillBGMusicSource
            : activeBGMusicSource;

        if (targetClip == null) return;

        // Only switch if needed
        if (audioSource.clip == targetClip) return;

        audioSource.clip = targetClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StartBGMusic()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();

        audioSource.volume = 1f;
    }

    public void StopBGMusic()
    {
        audioSource.volume = 0f;
    }
}