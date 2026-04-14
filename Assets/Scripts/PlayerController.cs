using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAiming))]
public class PlayerController : MonoBehaviour
{

    public PlayerMovement Movement { get; private set; }
    public PlayerAiming Aiming { get; private set; }

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Aiming = GetComponent<PlayerAiming>();
    }

    public void SetActive(bool active)
    {
        Movement.enabled = active;
        Aiming.enabled = active;

        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null) shooting.enabled = active;

        foreach (var mb in GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb == this) continue;
            if (mb == Movement) continue;
            if (mb == Aiming) continue;
            if (mb == shooting) continue;
            mb.enabled = active;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = !active;

        var col = GetComponentInChildren<Collider>();
        if (col != null) col.enabled = active;
    }
}