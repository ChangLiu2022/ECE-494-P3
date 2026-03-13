using UnityEngine;


public class PlayerAiming : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Settings")]
    [SerializeField] private float rotationSmoothing = 0f;


    public Vector3 AimPoint;

    public Vector3 AimDirection;

    private Plane groundPlane;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        groundPlane = new Plane(Vector3.up, Vector3.zero);
    }

    private void Update()
    {
        UpdateAimPoint();
        RotateTowardAim();
    }

    private void UpdateAimPoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float enter))
        {
            AimPoint = ray.GetPoint(enter);
        }

        Vector3 toAim = AimPoint - transform.position;
        toAim.y = 0f;

        if (toAim.sqrMagnitude > 0.001f)
            AimDirection = toAim.normalized;
    }

    private void RotateTowardAim()
    {
        if (AimDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(AimDirection, Vector3.up);

        transform.rotation = targetRot;

    }
}
