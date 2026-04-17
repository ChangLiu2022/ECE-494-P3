using UnityEngine;

public class OneWayDoors : MonoBehaviour
{
    private BoxCollider zone;
    private Transform player;
    private bool hasFired = false;

    private void Start()
    {
        zone = GetComponent<BoxCollider>();
        GameObject body = GameObject.FindWithTag("Player");
        if (body != null) player = body.transform;
    }

    private void Update()
    {
        if (hasFired || player == null) return;

        if (zone.bounds.Contains(player.position))
        {
            hasFired = true;
            EventBus.Publish(new GameEvents.PlayerEnteredMapEvent());
        }
    }
}