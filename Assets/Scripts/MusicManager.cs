using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static GameEvents;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip safehouseMusic;
    [SerializeField] private AudioClip chillBGMusic;
    [SerializeField] private AudioClip activeBGMusic;

    [Header("AudioSources")]
    [SerializeField] private AudioSource safehouseTrack;
    [SerializeField] private AudioSource chillTrack;
    [SerializeField] private AudioSource activeTrack;

    private string lastScene;
    private bool isActive = false;

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

        // Start muted, but playing
        safehouseTrack.volume = 0f;
        safehouseTrack.clip = safehouseMusic;
        safehouseTrack.loop = true;
        safehouseTrack.Play();

        chillTrack.volume = 0f;
        chillTrack.clip = chillBGMusic;
        chillTrack.loop = true;
        chillTrack.Play();

        activeTrack.volume = 0f;
        activeTrack.clip = activeBGMusic;
        activeTrack.loop = true;
        activeTrack.Play();
    }

    void OnEnable()
    {
      EventBus.Subscribe<WinEvent>(e => StopBGMusic());  
      EventBus.Subscribe<StopMusicEvent>(e => StopBGMusic());
      EventBus.Subscribe<TimerExpiredEvent>(e => SwitchMusic());
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<WinEvent>(e => StopBGMusic());
        EventBus.Unsubscribe<StopMusicEvent>(e => StopBGMusic());
        EventBus.Unsubscribe<TimerExpiredEvent>(e => SwitchMusic());
    }

    void Start()
    {
        lastScene = SceneManager.GetActiveScene().name;
        ApplyMusic();
    }

    void Update()
    {
        // CHEATS
        if (Input.GetKeyDown(KeyCode.Equals)) EventBus.Publish(new SwitchMusicEvent());

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != lastScene)
        {
            lastScene = currentScene;
            ApplyMusic();
        }
    }

    private void ApplyMusic()
    {
        //if (lastScene == "Safehouse") JustSafehouse(0.1f);
        //else if (lastScene.StartsWith("CutScene") || lastScene == "Main Menu") JustSafehouse(0f); 
        if (lastScene == "Safehouse")
        {
            // do nothing
        }
        else if (lastScene.StartsWith("CutScene") || lastScene == "Main Menu")
        {
            // also do nothing
        }
        else
        {
            isActive = false;
            SwitchMusic();
        }
    }

    public void StopBGMusic() { JustSafehouse(0f); }

    private void JustSafehouse(float volume)
    {
        if (safehouseTrack == null) return;
        if (chillTrack == null) return;
        if (activeTrack == null) return;

        safehouseTrack.volume = volume;
        chillTrack.volume = 0f;
        activeTrack.volume = 0f;
    }

    private void SwitchMusic()
    {
        if (safehouseTrack == null) return;
        if (chillTrack == null) return;
        if (activeTrack == null) return;

        isActive = !isActive;

        safehouseTrack.volume = 0f;
        chillTrack.volume = isActive ? 0.5f : 0f;
        activeTrack.volume = isActive ? 0f : 0.5f;
    }
}