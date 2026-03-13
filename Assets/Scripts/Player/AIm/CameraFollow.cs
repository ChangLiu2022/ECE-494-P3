using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -8f);
    [SerializeField] private bool useLERP = false;
    [SerializeField] private float lerpSpeed = 100f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        transform.position = useLERP
            ? Vector3.Lerp(transform.position, desired, lerpSpeed * Time.deltaTime)
            : desired;
    }
}