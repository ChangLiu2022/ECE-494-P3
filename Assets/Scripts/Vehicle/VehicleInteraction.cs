using UnityEngine;


public class VehicleInteraction : MonoBehaviour
{
    [SerializeField] private Transform exitPoint;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private VehicleMovement movement;

    public bool IsOccupied { get; private set; }
    public PlayerController Driver { get; private set; }

    private Crosshair _crosshair;

    private void Awake()
    {
        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();
        if (movement == null)
            movement = GetComponentInChildren<VehicleMovement>();
        _crosshair = FindObjectOfType<Crosshair>();
    }

    private Transform MovingPart => (movement != null) ? movement.transform : transform;

    public void Enter(PlayerController player)
    {
        IsOccupied = true;
        Driver = player;

        player.SetActive(false);
        SetPlayerVisible(player, false);

        if (_crosshair != null)
            _crosshair.gameObject.SetActive(false);

        player.transform.SetParent(MovingPart);
        player.transform.localPosition = Vector3.zero;

        if (cameraFollow != null)
            cameraFollow.SetTarget(MovingPart);

        if (movement != null)
            movement.SetActive(true);
    }

    public void Exit()
    {
        var player = Driver;
        IsOccupied = false;
        Driver = null;

        if (movement != null)
            movement.SetActive(false);

        player.transform.SetParent(null);

        Vector3 exitPos = (exitPoint != null) ? exitPoint.position : MovingPart.position + MovingPart.right * 2f;
        player.transform.position = exitPos;

        player.SetActive(true);
        SetPlayerVisible(player, true);

        if (_crosshair != null)
            _crosshair.gameObject.SetActive(true);

        player.Movement.SetVelocity(Vector3.zero);

        if (cameraFollow != null)
            cameraFollow.SetTarget(player.transform);
    }

    private void SetPlayerVisible(PlayerController player, bool visible)
    {
        foreach (var r in player.GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }
}