using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;

    [SerializeField] private LayerMask hit_mask;

    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.velocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // raycast ahead by how far the bullet moves this frame
        // catches thin walls that triggers skip over
        float step = speed * Time.fixedDeltaTime;

        if (Physics.Raycast(
            transform.position,
            rb.velocity.normalized,
            out RaycastHit hit,
            step,
            hit_mask))
        {
            HandleHit(hit.collider);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        // only react to things on the hit_mask
        if (((1 << other.gameObject.layer) & hit_mask) == 0)
            return;

        HandleHit(other);
    }


    private void HandleHit(Collider other)
    {
        // game over
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Player was shot! Game Over.");
            EventBus.Publish(new GameEvents.GameOverEvent());
            Destroy(gameObject);
            return;
        }

        // guard was hit
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("Guard hit: " + other.gameObject.name);

            GuardController guard =
                other.transform.root.GetComponent<GuardController>();

            if (guard != null)
                guard.TakeDamage(transform.forward);

            Destroy(gameObject);
            return;
        }


        // wall or anything else on the mask = destroy bullet
        Destroy(gameObject);
    }
}