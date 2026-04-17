using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static GameEvents;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip safehouseMusic; // Optional: assign in inspector for fallback
    [SerializeField] private AudioClip chillBGMusic; // Optional: assign in inspector for fallback
    [SerializeField] private AudioClip activeBGMusic; // Optional: assign in inspector for fallback

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
        chillTrack.volume = 0f;
        activeTrack.volume = 0f;

        safehouseTrack.clip = safehouseMusic;
        chillTrack.clip = chillBGMusic;
        activeTrack.clip = activeBGMusic;

        safehouseTrack.loop = true;
        chillTrack.loop = true;
        activeTrack.loop = true;

        safehouseTrack.Play();
        chillTrack.Play();
        activeTrack.Play();
    }

    void OnEnable()
    {
      EventBus.Subscribe<WinEvent>(e => StopBGMusic());  
      EventBus.Subscribe<SwitchMusicEvent>(e => SwitchMusic());
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<WinEvent>(e => StopBGMusic());
        EventBus.Unsubscribe<SwitchMusicEvent>(e => SwitchMusic());
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

        if (safehouseTrack.volume == 0.333f)
        {
            Debug.LogWarning("Safehouse volume became 0.33 HERE", this);
        }
    }

    private void ApplyMusic()
    {
        if (lastScene == "Safehouse")
        {
            Debug.Log("We're in the safehouse!\n" + lastScene.ToString());
            JustSafehouse(0.2f);
        }
        else if (lastScene.StartsWith("CutScene") || lastScene == "Main Menu")
        {
            Debug.Log("We're in a cutscene!\n" + lastScene.ToString());
            JustSafehouse(0f);
        }
        else
        {
            Debug.Log("We're in a level!\n" + lastScene.ToString());
            isActive = false;
            SwitchMusic();
            isActive = false;
        }
    }

    public void StopBGMusic()
    {
        JustSafehouse(0f);
    }

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

        safehouseTrack.volume = 0f;
        chillTrack.volume = 1f;
        activeTrack.volume = isActive ? 1f : 0f;

        isActive = !isActive;
    }
}