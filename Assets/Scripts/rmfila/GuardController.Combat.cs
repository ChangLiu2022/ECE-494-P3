using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public partial class GuardController
{
    // only shoot when tier 4, weapon drawn, and player visible
    private void ShootAtPlayer()
    {
        if (current_tier < GuardTier.Tier4 || is_drawing_weapon ||
            can_see_player == false || player == null)
        {
            return;
        }

        if (can_see_player == true)
            sight_timer += Time.fixedDeltaTime;

        else
            sight_timer = 0f;

        if (sight_timer < sight_delay)
            return;

        if (Vector3.Distance
            (transform.position, player.position) > shoot_range)
        {
            return;
        }

        if (Time.time < next_fire_time)
            return;

        next_fire_time = Time.time + fire_rate;

        Vector3 direction = 
            (player.position - fire_point.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject bullet_obj =
            Instantiate(bullet_prefab, fire_point.position, rotation);

        BulletMovement bullet = bullet_obj.GetComponent<BulletMovement>();

        if (bullet != null)
            bullet.Initialize(gameObject);
    }


    // handles bullet impact
    // damage, knockback, stagger
    private void TakeDamage(Collider bullet)
    {
        Vector3 knockback_dir = bullet.transform.forward;

        Destroy(bullet.gameObject);

        current_health--;

        if (current_health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // still alive, stagger so the player can escape
        if (is_staggered == false)
            StartCoroutine(StaggerRoutine(knockback_dir, -knockback_dir));
    }


    private void EndGame()
    {
        EventBus.Publish(new GameEvents.GameOverEvent());
    }


    private IEnumerator StaggerRoutine
        (Vector3 knockback_dir, Vector3 investigate_dir)
    {
        is_staggered = true;

        CancelSearchAndReturn();

        is_spotting = false;

        guard.ResetPath();

        sight_loss_direction = investigate_dir;
        player_last_position =
            transform.position + investigate_dir * (overshoot_distance * 2f);

        // raycast along the navmesh surface
        // stops naturally at wall boundaries
        Vector3 knockback_start = transform.position;
        Vector3 raw_target = 
            transform.position + knockback_dir * knockback_distance;
        Vector3 knockback_end = knockback_start;

        if (NavMesh.SamplePosition(raw_target, out NavMeshHit nav_hit,
            knockback_distance + 0.5f, NavMesh.AllAreas))
        {
            knockback_end = nav_hit.position;
        }

        float knockback_elapsed = 0f;
        float knockback_duration = 0.12f;

        while (knockback_elapsed < knockback_duration)
        {
            knockback_elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(knockback_elapsed / knockback_duration);
            guard.Warp(Vector3.Lerp(knockback_start, knockback_end, t));

            yield return null;
        }

        guard.Warp(knockback_end);
        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;

        if (can_see_player)
        {
            if (guns_out)
                TierUp(GuardTier.Tier4);

            else if (current_tier < GuardTier.Tier3)
                TierUp(GuardTier.Tier3);
        }

        else
        {
            current_chase_bar = chase_bar_max;
            is_investigating = true;

            if (guns_out)
                TierUp(GuardTier.Tier4);

            else
                TierUp(GuardTier.Tier3);
        }
    }

    // weapon draw delay, guard is tier 4 but can't shoot until complete
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
