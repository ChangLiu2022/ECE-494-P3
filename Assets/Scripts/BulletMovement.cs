using System;
using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 30f;
    
    [SerializeField] private ParticleSystem bloodEffectPrefab;
    private LayerMask raycast_mask;
    
    private Collider bullet_collider;
    private float aliveTime = 0f;
    private Rigidbody rb;
    private int damage = 1;

    public void SetDamage(int d) 
    { 
        damage = d; 
    }

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
        
        raycast_mask = Physics.DefaultRaycastLayers & ~(1 << owner.layer);

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

        // bullet before was hitting colliders and breaking stuff
        if (!Physics.Raycast(transform.position, 
            transform.forward, 
            out RaycastHit hit, 
            stepDistance, 
            raycast_mask, 
            QueryTriggerInteraction.Ignore))
        {
            rb.MovePosition(transform.position + transform.forward * stepDistance);
            return;
        }

        if (hit.collider.CompareTag("Body"))
        {
            EventBus.Publish(new GameEvents.GameOverEvent());
            Destroy(gameObject);
            return;
        }

        if (hit.collider.CompareTag("Enemy"))
        {
            var guard = hit.collider.GetComponentInParent<GuardController>();
            if (guard != null) guard.TakeDamage(bullet_collider);

            var dummy = hit.collider.GetComponentInParent<TrainingDummy>();
            if (dummy != null) dummy.TakeDamage(bullet_collider);
        }

        Destroy(gameObject);
    }
}