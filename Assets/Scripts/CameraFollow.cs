using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, 0f);
    [SerializeField] private Vector3 vehicleOffset = new Vector3(0f, 25f, 0f);
    [SerializeField] private bool useSmoothing = false;
    [SerializeField] private float smoothSpeed = 10f;

    private Vector3 _defaultOffset;
    private Vector3 _currentOffset;

    private void Awake()
    {
        _defaultOffset = offset;
        _currentOffset = offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + _currentOffset;

        transform.position = useSmoothing
            ? Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime)
            : desired;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetVehicleView() => _currentOffset = vehicleOffset;
    public void SetDefaultView() => _currentOffset = _defaultOffset;
}