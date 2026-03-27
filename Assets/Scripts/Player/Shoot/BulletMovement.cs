using System;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;

    private string owner_tag = "";
    private float aliveTime = 0f;
    private Rigidbody rb;

    public void Initialize(string owner)
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

        if ((owner_tag != "" && hit.collider.CompareTag(owner_tag)) || hit.collider.CompareTag("Floor") ||
            hit.collider.CompareTag("Enemy"))
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

        Debug.Log("Bullet destroyed by: " + hit.collider.gameObject.name);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.CompareTag("Floor"))
        {
            Destroy(gameObject);

        }
    }


    private void OnTriggerStay(Collider collision)
    {
        if (!collision.CompareTag("Floor"))
        {
            Destroy(gameObject);

        }
    }
}