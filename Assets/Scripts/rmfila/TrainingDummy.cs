using System.Collections;
using UnityEngine;
using static GameEvents;

public class TrainingDummy : MonoBehaviour
{
    [SerializeField] private int max_health = 999;
    [SerializeField] private float knockback_distance = 0.5f;
    [SerializeField] private float knockback_duration = 0.2f;
    [SerializeField] private ParticleSystem blood_effect;

    [SerializeField] private LayerMask wall_mask;
    private CapsuleCollider body_collider;

    private int current_health;
    private Coroutine knockback_routine;

    void Start()
    {
        current_health = max_health;
        body_collider = GetComponent<CapsuleCollider>();
    }


    public void TakeDamage(Collider bullet_col) => TakeDamage(bullet_col, 1);


    public void TakeDamage(Collider bullet_col, int dmg)
    {
        Vector3 direction = bullet_col.transform.forward;
        Destroy(bullet_col.gameObject);

        current_health -= Mathf.Max(1, dmg);
        EventBus.Publish(new GuardShotEvent());

        if (current_health <= 0) { Destroy(gameObject); return; }

        if (blood_effect != null)
        {
            blood_effect.transform.rotation =
                Quaternion.LookRotation(direction, Vector3.up);
            blood_effect.Play();
        }

        if (knockback_routine != null) StopCoroutine(knockback_routine);
        knockback_routine = StartCoroutine(KnockbackRoutine(direction));
    }


    private IEnumerator KnockbackRoutine(Vector3 direction)
    {
        Vector3 start = transform.position;
        Vector3 target = start + direction * knockback_distance;

        float radius = body_collider != null ? body_collider.radius : 0.1f;

        if (Physics.SphereCast(start, radius, direction, out RaycastHit hit, knockback_distance, wall_mask))
            target = hit.point + hit.normal * (radius + 0.05f);

        float timer = 0f;
        while (timer < knockback_duration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, timer / knockback_duration);
            yield return null;
        }

        transform.position = target;
    }
}
