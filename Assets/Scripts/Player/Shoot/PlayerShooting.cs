using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private PlayerAiming aiming;
    [SerializeField] private MonoBehaviour gun;

    private void Awake()
    {
        if (aiming == null)
            aiming = GetComponent<PlayerAiming>();
    }

    public Vector3 AimDirection => aiming.AimDirection;
}