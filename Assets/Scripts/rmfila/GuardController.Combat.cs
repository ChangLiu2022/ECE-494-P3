using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class GuardController
{
    private void ShootAtPlayer()
    {
        // only shoot when tier 4, weapon drawn, and player visible
        if (current_tier < GuardTier.Tier4 || is_drawing_weapon ||
            !can_see_player || player == null)
            return;

        sight_timer += Time.fixedDeltaTime;
        if (sight_timer < sight_delay)
            return;

        if (Vector3.Distance(transform.position, player.position) > shoot_range)
            return;

        if (Time.time < next_fire_time)
            return;

        next_fire_time = Time.time + fire_rate;

        Vector3 direction = (player.position - fire_point.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject bullet_obj = Instantiate(bullet_prefab, fire_point.position, rotation);
        BulletMovement bullet = bullet_obj.GetComponent<BulletMovement>();
        if (bullet != null)
            bullet.Initialize(gameObject);
    }

    public void TakeDamage(Collider bullet)
    {
        Vector3 knockback_dir = bullet.transform.forward;
        Destroy(bullet.gameObject);
        current_health--;

        if (current_health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!is_staggered)
            StartCoroutine(StaggerRoutine(knockback_dir, player_last_position));
        
        
        if (bloodEffectPrefab != null)
        {
            bloodEffectPrefab.transform.rotation = Quaternion.LookRotation(knockback_dir, Vector3.up);
            bloodEffectPrefab.Play();
        }
    }

    private void EndGame()
    {
        EventBus.Publish(new GameEvents.GameOverEvent());
    }

    private IEnumerator StaggerRoutine(Vector3 knockback_dir, Vector3 investigate_position)
    {
        is_staggered = true;
        CancelAllRoutines();
        is_spotting = false;
        guard.enabled = false;

        sight_loss_direction = (investigate_position - transform.position).normalized;

        Vector3 start_pos = transform.position;
        Vector3 end_pos = start_pos + knockback_dir * knockback_distance;
        float knockback_time = 0f;
        float knockback_duration = 0.15f;

        while (knockback_time < knockback_duration)
        {
            knockback_time += Time.deltaTime;
            transform.position = Vector3.Lerp(start_pos, end_pos, knockback_time / knockback_duration);
            yield return null;
        }

        guard.enabled = true;
        guard.SetDestination(transform.position);
        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;
        current_chase_bar = chase_bar_max;

        if (!can_see_player)
            is_investigating = true;

        TierUp(GuardTier.Tier4);
    }

    private IEnumerator DrawWeaponRoutine()
    {
        is_drawing_weapon = true;
        yield return new WaitForSeconds(weapon_draw_time);
        if (guard_weapon != null)
        {
            guard_weapon.SetActive(true);
            guards_sprite_renderer.sprite = lethal_guard_sprite;
        }
        is_drawing_weapon = false;
    }
}