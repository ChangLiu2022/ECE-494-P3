using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAiming aiming;

    private void Awake()
    {
        Cursor.visible = false;
    }
    private void Update()
    {
        if (aiming == null) return;

        transform.position = aiming.AimPoint;
    }
}