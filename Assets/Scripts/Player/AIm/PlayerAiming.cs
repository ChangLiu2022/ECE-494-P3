using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 720f; // degrees per second
    [SerializeField] private LayerMask groundMask;

    public Vector3 AimPoint;
    public Vector3 AimDirection;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void FixedUpdate()
    {
        UpdateAimPoint();
        RotateTowardAim();
    }

    private void UpdateAimPoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            AimPoint = ray.GetPoint(enter);
        }

        Vector3 toAim = AimPoint - transform.position;
        toAim.y = 0f;

        if (toAim.sqrMagnitude > 0.001f)
        {
            AimDirection = toAim.normalized;
            Debug.DrawRay(transform.position, AimDirection * 5f, Color.red);
        }
    }

    private void RotateTowardAim()
    {
        if (AimDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(AimDirection, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }
}