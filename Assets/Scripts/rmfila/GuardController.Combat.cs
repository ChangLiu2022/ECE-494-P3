using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public partial class GuardController
{
    private void ShootAtPlayer()
    {
        // only shoot when tier 4, weapon drawn, and player visible
        if (current_tier < GuardTier.Tier4 || is_drawing_weapon ||
            can_see_player == false || player == null)
        {
            return;
        }

        // increment the amount of time the guard has
        // seen the player, used to give a moment before they
        // shoot, almost like they're lining up the shot
        if (can_see_player == true)
            sight_timer += Time.fixedDeltaTime;

        // reset if lost sight of player
        else
            sight_timer = 0f;

        // only shoot if we have seen the player for the delay time
        if (sight_timer < sight_delay)
            return;

        // if distance between guard and player is too far, do not shoot
        if (Vector3.Distance
            (transform.position, player.position) > shoot_range)
        {
            return;
        }

        // fire rate limit
        if (Time.time < next_fire_time)
            return;

        // add the fire_rate to the current time for next time we can fire
        next_fire_time = Time.time + fire_rate;

        Vector3 direction = 
            (player.position - fire_point.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        // bullet is instantiated going in the direction of the player
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
            // pass the knockback_direction and investigation direction to be
            // the opposite to where the bullet was traveling. this way the
            // guard investigates where the bullet came from
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

        // ----- TODO: make it so the guard can maybe navigate in the -----
        //       actual player's direction, not its assumed

        // we want to overshoot where the bullet came from to keep
        // looking in that direction, assuming the player ran away
        // in that direction too.
        player_last_position =
            transform.position + investigate_dir * (overshoot_distance * 2f);

        /* OLD!!!
         * used warp instead and didnt disrupt the guard's rotation
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
        */

        // NEW!!! -- apply knockback
        if (TryGetNavPoint(transform.position, knockback_dir,
            knockback_distance, 0.3f, out Vector3 knockback_end))
        {
            yield return MoveWithTimeout(knockback_end, 0.12f);
        }

        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;

        // reset the chase bar after stagger
        current_chase_bar = chase_bar_max;

        // if the player is not visible after being staggered
        if (!can_see_player)
            // investigate the last known area
            is_investigating = true;

        // guard is set to be lethal mode because they were shot
        TierUp(GuardTier.Tier4);
    }

    // weapon draw delay, guard is tier 4 but can't shoot until complete
    private IEnumerator DrawWeaponRoutine()
    {
        is_drawing_weapon = true;

        yield return new WaitForSeconds(weapon_draw_time);

        if (guard_weapon != null)
        {
            // after wait time, set the gun to be active
            guard_weapon.SetActive(true);
            // update the sprite
            guards_sprite_renderer.sprite = lethal_guard_sprite;
        }

        is_drawing_weapon = false;
    }
}
