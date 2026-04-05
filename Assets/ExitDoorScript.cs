using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;

public class ExitDoorScript : MonoBehaviour
{
    [SerializeField] public float interactRange = 1.5f;
    [SerializeField] private string target_scene = "Safehouse";
    [SerializeField] private bool set_tutorial_complete = false;
    [SerializeField] private bool set_newmap_complete = false;
    [SerializeField] private string no_gold_message = "You need to collect the gold before leaving!";

    private bool can_leave = false;
    private Transform player;

    private void OnEnable()
    {
        EventBus.Subscribe<GoldEvent>(OnGoldCollected);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GoldEvent>(OnGoldCollected);
    }

    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (can_leave)
            {
                if (set_tutorial_complete) SafehouseState.completed_tutorial = true;
                if (set_newmap_complete) SafehouseState.completed_newmap = true;

                SceneManager.LoadScene(target_scene);
            }
            else
            {
                InformationBoxController.instance.Show(no_gold_message);
            }
        }
    }

    private void OnGoldCollected(GoldEvent e)
    {
        can_leave = true;
    }
}
