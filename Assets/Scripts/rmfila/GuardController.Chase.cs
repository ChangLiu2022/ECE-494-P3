using UnityEngine;


// partial class of guard controller, break that thing down
public partial class GuardController
{
    // handles the freeze-and-fill phase before a chase begins
    private void HandleSpotting()
    {
        guard.ResetPath();

        // always face the player when spotting them
        // it looks like the guard is going "hmmm what is that"
        FaceTarget(player.position);

        if (can_see_player && player != null)
        {
            // update the last known position and direction to be what
            // the guard sees so when the chase starts, they have a valid
            // starting point
            player_last_position = player.position;

            Vector3 spot_diff = player.position - player_previous_position;

            // only update the direction if the change is substantial enough
            if (spot_diff.sqrMagnitude > 0.01f)
                player_last_direction = spot_diff.normalized;

            player_previous_position = player.position;

            // fill rate scales exponentially with proximity.
            // at max range multiplier is 1, closest range its 1 + exponent
            float normalized_proximity = 1f - Mathf.Clamp01(
                sight_distance / max_detect_radius);

            float fill_multiplier = 1f + proximity_exponent *
                (normalized_proximity * normalized_proximity);

            // update the chase bar
            current_chase_bar = Mathf.Min(current_chase_bar + 
                chase_bar_fill_rate * fill_multiplier * Time.fixedDeltaTime,
                chase_bar_max);

            // check if the bar is full
            if (current_chase_bar >= chase_bar_max)
            {
                // no longer spotting but activating a chase
                is_spotting = false;

                if (guns_out)
                    TierUp(GuardTier.Tier4);

                else
                    TierUp(GuardTier.Tier3);
            }
        }

        else
        {
            // player left the cone, decay the bar back to zero
            current_chase_bar = Mathf.Max(current_chase_bar - 
                chase_bar_decay * Time.fixedDeltaTime, 0f);

            // no longer spotting, but returning to normal,
            if (current_chase_bar <= 0f)
            {
                is_spotting = false;
                
                static_search_routine = StartCoroutine(ResumeIdleRoutine());
            }
        }
    }


    // can only move up, ErraticDrop is the way to downgrade
    private void TierUp(GuardTier tier)
    {
        if (tier <= current_tier)
            return;

        current_tier = tier;
        current_chase_bar = chase_bar_max;
        had_sight = true;
        sight_loss_timer = pursuit_window;

        if (tier == GuardTier.Tier4)
        {
            guns_out = true;

            if (is_drawing_weapon == false)
                StartCoroutine(DrawWeaponRoutine());
        }

        ApplySpeed();
    }

    private void ErraticDrop()
    {
        current_tier = GuardTier.Tier2;
        current_chase_bar = 0f;
        ApplySpeed();

        if (guard_mode != GuardMode.Patrol)
            static_search_routine = StartCoroutine(StaticScanRoutine());

        // alert every other guard in the building to go erratic
        EventBus.Publish(new GameEvents.ErraticAlertEvent());
    }

    private void ApplySpeed()
    {
        if (current_tier == GuardTier.Tier1)
            guard.speed = patrol_speed;

        else
            guard.speed = erratic_speed;
    }


    // tracks position + direction while guard has sight, freezes on loss.
    // player_last_position only updates here if the guard can see the player
    private void UpdateChaseTracking()
    {
        if (can_see_player && player != null)
        {
            // save the latest information of the player to
            // use when we lose sight
            had_sight = true;
            // reset timer to full
            sight_loss_timer = pursuit_window;
            player_last_position = player.position;

            Vector3 difference = player.position - player_previous_position;

            if (difference.sqrMagnitude > 0.01f)
            {
                player_last_direction = difference.normalized;

                // guess player speed for predicted position on sight loss
                player_estimated_speed = 
                    difference.magnitude / Time.fixedDeltaTime;
            }

            // update the tracking formation
            player_previous_position = player.position;
            current_chase_bar = chase_bar_max;
            return;
        }

        // lost sight, keep predicting where the player went.
        // player_last_position moves forward each frame so the guard
        // chases a moving predicted target instead of freezing at the corner
        if (sight_loss_timer > 0f)
        {
            // decrement timer on sight loss
            sight_loss_timer -= Time.fixedDeltaTime;

            // while timer is active, keep pushing the player's
            // last_position so the guard chases a moving predicted
            // point rather than freezing at a corner where sight was loss
            if (player_estimated_speed > 0.1f)
                player_last_position += player_last_direction * 
                    player_estimated_speed * Time.fixedDeltaTime;
        }

        // we are in the exact frame sight was lost
        if (had_sight)
            had_sight = false;

        // capture the point to later use for the point to overshoot
        if (had_sight_last_frame && can_see_player == false)
            sight_loss_direction =
                (player_last_position - transform.position).normalized;

        had_sight_last_frame = can_see_player;
    }

    // moves guard towards last known position
    private void ChasePlayer()
    {
        Vector3 target_destination;

        if (can_see_player)
            target_destination = player.position;

        else
            target_destination = player_last_position;

        if (Vector3.Distance(target_destination, current_destination) > 0.2f)
        {
            guard.SetDestination(target_destination);
            current_destination = target_destination;
        }

        // track the direction the guard is actually moving so overshoot
        // continues in the right direction
        if (guard.velocity.sqrMagnitude > 0.01f)
            last_chase_velocity = guard.velocity.normalized;

        if (can_see_player && player != null)
            FaceTarget(player.position);

        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();

        else
            FaceTarget(player_last_position);

        // reached last known position with no sight, begin search.
        // don't start until the pursuit window has closed
        if (can_see_player == false && sight_loss_timer <= 0f &&
            HasReachedDestination() && active_routine == null)
        {
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }

    // walks to player_last_position regardless of chase bar logic
    private void Investigate()
    {
        if (Vector3.Distance(player_last_position, current_destination) > 0.2f)
        {
            guard.SetDestination(player_last_position);
            current_destination = player_last_position;
        }

        if (can_see_player && player != null)
            FaceTarget(player.position);

        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();

        else
            FaceTarget(player_last_position);

        investigate_timer += Time.deltaTime;
        if ((HasReachedDestination() || investigate_timer >= 
            investigate_timeout) && active_routine == null)
        {
            is_investigating = false;

            if (sight_loss_direction.sqrMagnitude < 0.01f)
            {
                if (last_chase_velocity.sqrMagnitude > 0.01f)
                    sight_loss_direction = last_chase_velocity;

                else
                    sight_loss_direction = transform.forward;
            }

            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    private void Patrol()
    {
        if (target_point == null)
            return;

        if (is_paused)
        {
            FaceTarget(target_point.position);
            pause_time -= Time.fixedDeltaTime;

            Vector3 to_target =
                (target_point.position - transform.position).normalized;

            bool facing_target =
                Quaternion.Angle(transform.rotation,
                YawFromDirection(to_target)) < 1f;

            if (pause_time <= 0f && facing_target)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }

            return;
        }

        FaceMovementDirection();

        if (HasReachedDestination())
        {
            is_paused = true;
            pause_time = pause_duration;
            guard.ResetPath();

            if (target_point == point_a)
                target_point = point_b;

            else
                target_point = point_a;
        }
    }
}
