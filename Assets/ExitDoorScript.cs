using UnityEngine;
using static GameEvents;

public class ExitDoorScript : MonoBehaviour
{
    [SerializeField] private Transform teleport_position;
    [SerializeField] public float interactRange = 1.5f;
    [SerializeField] private CameraFollow cam;

    private bool can_leave = false;
    private bool has_teleported = false;
    private Transform player;

    private void OnEnable()
    {
        EventBus.Subscribe<GoldEvent>(OnGoldCollected);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GoldEvent>(OnGoldCollected);
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (has_teleported) 
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            if (can_leave)
            {
                player.position = teleport_position.position;
                cam.SetTarget(player);
            }

            else
                Debug.Log("Door locked — gold not collected yet.");
        }
    }

    private void OnGoldCollected(GoldEvent e)
    {
        can_leave = true;
    }
}