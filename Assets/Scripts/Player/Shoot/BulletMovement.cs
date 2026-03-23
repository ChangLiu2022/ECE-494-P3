using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;

    private string owner_tag = "";


    public void Initialize(string owner)
    {
        owner_tag = owner;
    }


    private void Start()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.velocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (((1 << other.gameObject.layer) & hitMask) != 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // skip anything tagged the same as whoever fired this bullet
        if (owner_tag != "" && other.CompareTag(owner_tag))
            return;

        // skip the floor entirely
        if (other.CompareTag("Floor"))
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player was shot! Game Over.");
            EventBus.Publish(new GameEvents.GameOverEvent());
            Destroy(gameObject);
            return;
        }

        // if its hits walls and guards destroy it so it doesn't
        // do the banana peel effect
        Debug.Log("Bullet destroyed by: " + other.gameObject.name + " | Tag: " + other.tag);

        Destroy(gameObject);
    }
}