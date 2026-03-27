using System;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;

    private Collider bullet_collider;
    private float aliveTime = 0f;
    private Rigidbody rb;

    public void Initialize(GameObject owner)
    {
        bullet_collider = GetComponent<Collider>();

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
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        aliveTime += Time.fixedDeltaTime;
        if (aliveTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float stepDistance = speed * Time.fixedDeltaTime;

        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, stepDistance))
        {
            rb.MovePosition(transform.position + transform.forward * stepDistance);
            return;
        }

        rb.MovePosition(transform.position + transform.forward * stepDistance);

        if (hit.collider.CompareTag("Body"))
        {
            Debug.Log("Player was shot! Game Over.");
            EventBus.Publish(new GameEvents.GameOverEvent());
            Destroy(gameObject);
            return;
        }

        if (hit.collider.CompareTag("Enemy"))
        {
            hit.collider.GetComponentInParent<GuardController>().TakeDamage(bullet_collider);
            return;
        }

        Debug.Log("Bullet destroyed by: " + hit.collider.gameObject.name);
        Destroy(gameObject);
    }
}