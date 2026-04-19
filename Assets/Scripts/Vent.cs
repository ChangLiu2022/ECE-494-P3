using UnityEngine;

public class Vent : MonoBehaviour
{
    public float interactRange = 1.5f;
    public bool isEntrance;
    public float ventYOffset = 2f;
    [Tooltip("If set, teleports player here instead of using Y offset.")]
    public Transform teleportPoint;

    private static bool playerInVent = false;
    public static bool PlayerInVent => playerInVent;
    private static bool usedThisFrame = false;
    private bool can_run = false;
    private Transform player;
    private Camera cam;

    void Start()
    {
        playerInVent = false;
    }

    void LateUpdate()
    {
        usedThisFrame = false;

        player = GameObject.FindWithTag("Player").transform;

        if (player == null)
        {
            Debug.LogError("Player not found in scene! Make sure the player GameObject has the 'Player' tag.");
            return;
        }

        cam = Camera.main;

        can_run = true;
    }

    void Update()
    {
        if (can_run == false)
        {
            return;
        }

        if (usedThisFrame) return;

        if (isEntrance == playerInVent) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            usedThisFrame = true;

            if (teleportPoint != null)
                player.position = teleportPoint.position;
            else
                player.position += new Vector3(0, isEntrance ? ventYOffset : -ventYOffset, 0);

            playerInVent = isEntrance;

            if (isEntrance)
                cam.cullingMask |= (1 << LayerMask.NameToLayer("Vent"));
            else
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("Vent"));
        }
    }
}