using UnityEngine;

public class Vent : MonoBehaviour
{
    public float interactRange = 1.5f;
    public bool isEntrance;
    public float ventYOffset = 2f;

    private static bool playerInVent = false;
    private static bool usedThisFrame = false;

    private Transform player;
    private Camera cam;

    void Start()
    {
        playerInVent = false;
        player = GameObject.FindWithTag("Body").transform;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        usedThisFrame = false;
    }

    void Update()
    {
        if (usedThisFrame) return;
        if (isEntrance == playerInVent) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            usedThisFrame = true;
            float yOffset = isEntrance ? ventYOffset : -ventYOffset;
            player.position += new Vector3(0, yOffset, 0);
            playerInVent = isEntrance;

            if (isEntrance)
                cam.cullingMask |= (1 << LayerMask.NameToLayer("Vent"));
            else
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer("Vent"));
        }
    }
}