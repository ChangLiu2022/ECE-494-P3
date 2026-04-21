using UnityEngine;

public class MenuFloatyMotion : MonoBehaviour
{
    [Header("Rotation (Teeter-Totter)")]
    [SerializeField] private float rotationAmplitude = 5f;   // degrees
    [SerializeField] private float rotationSpeed = 1.5f;

    [Header("Scale (Breathing Effect)")]
    [SerializeField] private float scaleAmplitude = 0.05f;   // % of original scale
    [SerializeField] private float scaleSpeed = 1.2f;

    [Header("Position Drift")]
    [SerializeField] private float positionAmplitude = 5f;   // units (UI: pixels)
    [SerializeField] private float positionSpeed = 0.8f;

    private Vector3 initialRotation;
    private Vector3 initialScale;
    private Vector3 initialPosition;

    private float randomOffset;

    void Awake()
    {
        initialRotation = transform.eulerAngles;
        initialScale = transform.localScale;
        initialPosition = transform.localPosition;

        // random offset so multiple objects don't sync perfectly
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = Time.time + randomOffset;

        // --- Rotation (Z axis for "see-saw" effect) ---
        float rotationZ = Mathf.Sin(time * rotationSpeed) * rotationAmplitude;
        transform.eulerAngles = new Vector3(
            initialRotation.x,
            initialRotation.y,
            initialRotation.z + rotationZ
        );

        // --- Scale (uniform pulse) ---
        float scaleOffset = 1f + Mathf.Sin(time * scaleSpeed) * scaleAmplitude;
        transform.localScale = initialScale * scaleOffset;

        // --- Position drift (subtle float) ---
        float posX = Mathf.Sin(time * positionSpeed) * positionAmplitude;
        float posY = Mathf.Cos(time * positionSpeed * 0.8f) * positionAmplitude;

        transform.localPosition = initialPosition + new Vector3(posX, posY, 0f);
    }
}