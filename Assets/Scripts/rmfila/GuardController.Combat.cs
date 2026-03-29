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
        sight_timer += Time.fixedDeltaTime;

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
    public void TakeDamage(Collider bullet)
    {
        Vector3 knockback_dir = bullet.transform.forward;

        // destroy the bullet here so we don't have to worry about double hit
        Destroy(bullet.gameObject);

        current_health--;

        // guard died
        if (current_health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // still alive, stagger so the player can escape
        if (is_staggered == false)
            // pass the knockback_direction and player_last_position
            // so this is where the guard will investigate
            StartCoroutine(StaggerRoutine
                (knockback_dir, player_last_position));
    }


    private void EndGame()
    {
        EventBus.Publish(new GameEvents.GameOverEvent());
    }


    private IEnumerator StaggerRoutine
        (Vector3 knockback_dir, Vector3 investigate_position)
    {
        is_staggered = true;

        // cancel any out retoutines
        CancelAllRoutines();

        // no longer can see, knocked out
        is_spotting = false;

        // disable agent so we can move freely
        guard.enabled = false;

        // the last direction we have of the player
        sight_loss_direction = 
            (investigate_position - transform.position).normalized;

        Vector3 start_pos = transform.position;
        Vector3 end_pos = start_pos + knockback_dir * knockback_distance;
        float knockback_time = 0f;
        float knockback_duration = 0.15f;

        while (knockback_time < knockback_duration)
        {
            knockback_time += Time.deltaTime;

            float percentage_done = knockback_time / knockback_duration;

            transform.position = 
                Vector3.Lerp(start_pos, end_pos, percentage_done);

            yield return null;
        }

        // turn back on the agent
        guard.enabled = true;
        guard.SetDestination(transform.position);

        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;

        // reset the chase bar after stagger
        current_chase_bar = chase_bar_max;

        // if the player is not visible after being staggered
        if (can_see_player == false)
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
