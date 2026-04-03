using System.Collections;
using UnityEngine;

public partial class GuardController
{
    private void ShootAtPlayer()
    {
        // cant shoot when staggered
        if (is_staggered == true)
            return;

        // is currently drawing their gun out or cannot see player
        // then the guard cannot shoot at player
        if (guns_out == false || is_drawing_weapon == true ||
            can_see_player == false || player == null)
            return;

        // can see player or has their gun out, increment sight_timer
        // regulating how long it takes till the guard can shoot the player
        // after drawing their gun
        sight_timer += Time.fixedDeltaTime;

        // cannot shoot until the sight_timer exceeds the sight_delay
        if (sight_timer < sight_delay)
            return;

        // make sure the shot is within range for the guard
        if (Vector3.Distance
            (transform.position, player.position) > shoot_range)
            return;

        // make sure the guard's firerate is still being taken into account
        if (Time.time < next_fire_time)
            return;

        next_fire_time = Time.time + fire_rate;

        // get the direcciton the shot needs to travel
        Vector3 direction = 
            (player.position - fire_point.position).normalized;

        // create a bullet object in that direction
        GameObject bullet_obj = Instantiate(
            bullet_prefab, 
            fire_point.position, 
            Quaternion.LookRotation(direction, Vector3.forward));

        BulletMovement bullet = bullet_obj.GetComponent<BulletMovement>();

        if (bullet != null) 
            bullet.Initialize(gameObject);
    }


    public void TakeDamage(Collider bullet_col)
    {
        // knockback in the direction the bullet was traveling
        Vector3 knockback_direction = bullet_col.transform.forward;

        Destroy(bullet_col.gameObject);

        current_health--;

        // if health is 0 or below, destroy the guard prefab
        if (current_health <= 0) 
        { 
            Destroy(gameObject); 
            return;
        }

        if (bloodEffectPrefab != null)
        {
            bloodEffectPrefab.transform.rotation = Quaternion.LookRotation(
                knockback_direction, 
                Vector3.forward
                );

            bloodEffectPrefab.Play();
        }

        // if staggered already, stagger again
        if (stagger_routine != null) 
            StopCoroutine(stagger_routine);

        stagger_routine = 
            StartCoroutine(StaggerRoutine(knockback_direction));
    }


    private IEnumerator StaggerRoutine(Vector3 knockback_direction)
    {
        is_staggered = true;

        // if in the process of drawing weapon while shot,
        // cancel the routine
        if (draw_weapon_routine != null)
        {
            StopCoroutine(draw_weapon_routine);
            draw_weapon_routine = null;
            is_drawing_weapon = false;
        }

        // actively doing some other routine besides stagger
        // do the same thing, cancel it
        if (active_routine != null) 
        { 
            StopCoroutine(active_routine); 
            active_routine = null; 
        }

        // no longer returning, routine was canceled
        is_returning = false;

        // tell the guard to stop
        guard.isStopped = true;
        guard.ResetPath();
        // we need to manually lerp the guard's transform.position
        // so untieing the nav meshes override
        guard.updatePosition = false;

        Vector3 start_position = transform.position;

        Vector3 target_position = 
            start_position + knockback_direction * knockback_distance;

        float timer = 0f;
        // lerp based on the ratio of the timer/knockback_duration
        // while the timer is less than kb_dur
        while (timer < knockback_duration)
        {
            timer += Time.deltaTime;

            transform.position = 
                Vector3.Lerp(
                    start_position, 
                    target_position, 
                    timer / knockback_duration
                );

            yield return null;
        }

        // let the nav mesh update the position
        guard.updatePosition = true;
        // warp the guard in place after knockback is done
        // this is just to ensure the guard is correctly
        // placed back to the navmesh properly
        guard.Warp(transform.position);
        guard.isStopped = false;

        // wait for the stagger duration
        yield return new WaitForSeconds(stagger_duration);

        is_staggered = false;
        sight_timer = 0f;
        next_fire_time = 0f;

        Alert();
    }

    private IEnumerator DrawWeaponRoutine()
    {
        is_drawing_weapon = true;

        yield return new WaitForSeconds(weapon_draw_time);

        // enables guns after draw time is done
        guns_out = true;

        // enable the weapon capabilities
        if (guard_weapon != null) 
            guard_weapon.SetActive(true);

        // change guard sprite to lethal
        guards_sprite_renderer.sprite = lethal_guard_sprite;
        is_drawing_weapon = false;
    }


    private void EndGame() => EventBus.Publish(new GameEvents.GameOverEvent());
}
