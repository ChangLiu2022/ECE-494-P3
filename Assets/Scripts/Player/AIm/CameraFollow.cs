using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -8f);
    [SerializeField] private bool useSmoothing = false;
    [SerializeField] private float smoothSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        transform.position = useSmoothing
            ? Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime)
            : desired;
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}