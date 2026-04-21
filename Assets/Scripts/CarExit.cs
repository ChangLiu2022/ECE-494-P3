using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;

public class CarExit : MonoBehaviour
{
    [SerializeField] private string next_scene = "Tutorial";
    [SerializeField] private string not_ready_message = "Collect the gun and map before leaving.";
    [SerializeField] private string map_not_picked_up_message = "Collect the new map before leaving.";

    [Header("Pickup Settings")]
    [SerializeField] private float pickup_range = 2.5f;
    [SerializeField] private KeyCode pickup_key = KeyCode.E;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject dog;

    [Header("State Settings")]
    [SerializeField] private bool set_tutorial_complete = false;
    [SerializeField] private bool set_newmap_complete = false;
    [SerializeField] private bool set_map2_complete = false;
    [SerializeField] private bool set_final_map_complete = false;

    [Header("Driving Settings")]
    [SerializeField] private float distance = 5f;   // how far to move
    [SerializeField] private float speed = 2f;      // units per second
    [SerializeField] private bool useLocalRight = true; // local vs world right

    [Header("Audio Settings")]
    [SerializeField] private AudioClip engine_starting;
    [SerializeField] private AudioClip car_driving;

    [Header("Safehouse Car Settings")]
    [SerializeField] private bool is_safehouse_car = false;
    [SerializeField] private string[] scenes;

    private Coroutine moveRoutine;
    private bool in_range = false;
    private AudioSource audio;
    private static int current_scene = 0;
    private HUDController currentHUD;
    private MapController mapController;

    private void Awake()
    {
        audio = GetComponent<AudioSource>();
        if (audio == null) Debug.Log("No AudioSource component found on Car!");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FindCanvasNextFrame(scene.name));
    }

    private IEnumerator FindCanvasNextFrame(string sceneName)
    {
        yield return null; // wait 1 frame to ensure scene objects are initialized

        currentHUD = FindObjectOfType<HUDController>(true);

        if (currentHUD == null)
        {
            Debug.Log("HUDController not found in scene: " + sceneName);
        }

        mapController = FindObjectOfType<MapController>(true);

        if (mapController == null)
        {
            Debug.Log("Map controller not found in scene: " + sceneName);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f || player == null) return;

        float dist = Vector3.Distance(transform.position, player.GetComponent<Transform>().position);
        in_range = dist <= pickup_range;

        if (in_range && Input.GetKeyDown(pickup_key))
        {
            if (!PlayerWallet.level_reward_claimed && SceneManager.GetActiveScene().name != "Safehouse")
            {
                InformationBoxController.instance.Show("You need to rescue the dog before leaving!");
                return;
            }

            RunExit();
        }
    }

    private void RunExit()
    {
        if (!SafehouseState.gun_collected || !SafehouseState.paper_collected_once)
        {
            InformationBoxController.instance.Show(not_ready_message);
            return;
        }

        if (!SafehouseState.paper_collected)
        {
            InformationBoxController.instance.Show(map_not_picked_up_message);
            return;
        }

        if (set_tutorial_complete)
        {
            SafehouseState.completed_tutorial = true;
        }
            

        if (set_newmap_complete)
            SafehouseState.completed_newmap = true;

        if (set_map2_complete)
            SafehouseState.completed_map_2 = true;

        if (set_final_map_complete)
            SafehouseState.completed_final_map = true;

        EventBus.Publish(new PlayerDisabledEvent());
        player.SetActive(false);
        if (currentHUD != null) currentHUD.enabled = false;
        if (mapController != null) mapController.enabled = false;

        if (dog != null) 
            dog.SetActive(false);

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRight());
        StartCoroutine(PlayAudio());

        //FadeManager.Instance.StartTransition(tutorial_scene, null, 1.95f);
    }

    private IEnumerator PlayAudio()
    {
        if (engine_starting == null) yield break;


        if (next_scene == "Safehouse") audio.pitch = 1.65f;
        audio.PlayOneShot(engine_starting);
        yield return new WaitForSecondsRealtime(engine_starting.length / audio.pitch);

        if (car_driving != null) audio.PlayOneShot(car_driving);
    }

    private IEnumerator MoveRight()
    {
        yield return new WaitForSecondsRealtime(engine_starting.length-0.5f);

        Vector3 startPos = transform.position;
        Vector3 direction = useLocalRight ? transform.right : Vector3.right;
        Vector3 targetPos = startPos + direction.normalized * distance;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        // Snap exactly to target at the end
        transform.position = targetPos;
        moveRoutine = null;

        if (is_safehouse_car)
        {
            next_scene = scenes[Mathf.Min(current_scene, scenes.Length-1)];
            current_scene++;
        }

        else
        {
            PlayerWallet.AdvanceLevel();
        }

            FadeManager.Instance.StartTransition(next_scene, null, 1.95f, audio);
    }
}
