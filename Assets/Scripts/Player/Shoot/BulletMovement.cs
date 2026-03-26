using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;

    private string owner_tag = "";


    public void Initialize(GameObject owner)
    {
        Collider bullet_collider = GetComponent<Collider>();

        if (bullet_collider == null)
            return;

        // grab every collider on the owner and its children
        // covers the guard's sphere collider, agent, etc
        Collider[] owner_colliders =
            owner.GetComponentsInChildren<Collider>();

        for (int i = 0; i < owner_colliders.Length; i++)
            Physics.IgnoreCollision(bullet_collider, owner_colliders[i]);
    }


    private void Start()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.velocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Floor"))
            return;

        if (other.CompareTag("Body"))
        {
            Debug.Log("Player was shot! Game Over.");
            EventBus.Publish(new GameEvents.GameOverEvent());
            Destroy(gameObject);
            return;
        }

        // walls, guards hit by another guard's bullet, etc.
        Destroy(gameObject);
    }
}