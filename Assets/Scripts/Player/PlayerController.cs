using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAiming))]
public class PlayerController : MonoBehaviour
{
    public PlayerMovement Movement;
    public PlayerAiming Aiming;

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Aiming   = GetComponent<PlayerAiming>();
    }
}
