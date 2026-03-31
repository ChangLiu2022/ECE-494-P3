using UnityEngine;

public partial class GuardController
{
    private void HandleSpotting()
    {
        // FIX: only call ResetPath once when spotting begins, not every single frame.
        // the original called it every FixedUpdate which interrupted returning/searching guards.
        if (!spotting_reset_done)
        {
            if (!is_searching && !is_returning && static_search_routine == null)
                guard.ResetPath();
            spotting_reset_done = true;
        }

        // always face the player when spotting
        FaceTarget(player.position);

        if (can_see_player && player != null)
        {
            UpdatePlayerTracking();

            float normalized_proximity = 1f - Mathf.Clamp01(sight_distance / max_detect_radius);
            float fill_multiplier = 1f + proximity_exponent *
                (normalized_proximity * normalized_proximity);

            current_chase_bar = Mathf.Min(
                current_chase_bar + chase_bar_fill_rate * fill_multiplier * Time.fixedDeltaTime,
                chase_bar_max);

            if (current_chase_bar >= chase_bar_max)
            {
                is_spotting = false;
                spotting_reset_done = false;
                if (guns_out)
                    TierUp(GuardTier.Tier4);
                else
                    TierUp(GuardTier.Tier3);
            }
        }
        else
        {
            current_chase_bar = Mathf.Max(current_chase_bar - chase_bar_decay * Time.fixedDeltaTime, 0f);
            if (current_chase_bar <= 0f)
            {
                is_spotting = false;
                spotting_reset_done = false;
                if (active_routine == null)
                    static_search_routine = StartCoroutine(ResumeIdleRoutine());
            }
        }
    }

    private void ChasePlayer()
    {
        if (can_see_player)
        {
            SetGuardDestination(player.position);
            // reset stuck timer whenever we have vision - guard is actively pursuing
            chase_stuck_timer = 0f;
            chase_stuck_check_pos = transform.position;
        }
        else
        {
            SetGuardDestination(player_last_position);

            // FIX: stuck detection.
            // if the guard hasn't moved 0.5 units in CHASE_STUCK_THRESHOLD seconds,
            // they are blocked by geometry and will never reach the destination.
            // start searching instead of looping forever.
            if (Vector3.Distance(transform.position, chase_stuck_check_pos) > 0.5f)
            {
                chase_stuck_timer = 0f;
                chase_stuck_check_pos = transform.position;
            }
            else
            {
                chase_stuck_timer += Time.fixedDeltaTime;
                if (chase_stuck_timer >= CHASE_STUCK_THRESHOLD && active_routine == null)
                {
                    chase_stuck_timer = 0f;
                    sight_loss_direction = transform.forward;
                    active_routine = StartCoroutine(SearchAndReturnRoutine());
                    return;
                }
            }
        }

        FaceChaseDirection(player.position, player_last_position);

        // reached last known position with no sight - begin search
        if (!can_see_player && sight_loss_timer <= 0f &&
            HasReachedDestination() && active_routine == null)
        {
            chase_stuck_timer = 0f;
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }

    private void UpdateChaseTracking()
    {
        if (can_see_player && player != null)
        {
            sight_loss_timer = pursuit_window;
            UpdatePlayerTracking();
            current_chase_bar = chase_bar_max;
            had_sight_last_frame = true;
            return;
        }

        // FIX: removed the player_estimated_speed prediction that existed here.
        // that was the root cause of guards running to random spots.
        // on first detection, player_previous_position was from Start() (spawn point),
        // making estimated speed wildly wrong and launching the guard off the map.
        // the overshoot in SearchAndReturnRoutine already handles the
        // "chase around the corner" behavior - we don't need prediction too.

        // capture the sight loss position on the frame we lose sight
        if (had_sight_last_frame)
        {
            actual_sight_loss_position = player_last_position;
            sight_loss_direction = (player_last_position - transform.position).sqrMagnitude > 0.01f
                ? (player_last_position - transform.position).normalized
                : player_last_direction;
        }

        had_sight_last_frame = false;

        if (sight_loss_timer > 0f)
            sight_loss_timer -= Time.fixedDeltaTime;
    }

    private void UpdatePlayerTracking()
    {
        player_last_position = player.position;
        Vector3 difference = player.position - player_previous_position;
        // only update direction if movement is meaningful
        if (difference.sqrMagnitude > 0.0001f)
            player_last_direction = difference.normalized;
        player_previous_position = player.position;
    }

    // can only move up, ErraticDrop is the way to downgrade
    private void TierUp(GuardTier tier)
    {
        if (tier <= current_tier)
            return;

        current_tier = tier;
        current_chase_bar = chase_bar_max;
        sight_loss_timer = pursuit_window;

        // FIX: reset player tracking so the new chase starts with accurate direction data
        if (player != null)
        {
            player_previous_position = player.position;
            player_last_position = player.position;
        }

        if (tier >= GuardTier.Tier3)
            CancelAllRoutines();

        if (static_search_routine != null)
        {
            StopCoroutine(static_search_routine);
            static_search_routine = null;
        }

        if (tier == GuardTier.Tier4)
        {
            guns_out = true;
            if (!is_drawing_weapon)
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
        {
            if (static_search_routine != null)
                StopCoroutine(static_search_routine);
            static_search_routine = StartCoroutine(StaticScanRoutine());
        }

        EventBus.Publish(new GameEvents.ErraticAlertEvent());
    }

    private void ApplySpeed()
    {
        if (current_tier == GuardTier.Tier1)
            guard.speed = patrol_speed;
        else
            guard.speed = erratic_speed;
    }

    private void SetGuardDestination(Vector3 target)
    {
        if (Vector3.Distance(target, current_destination) > 0.2f)
        {
            guard.SetDestination(target);
            current_destination = target;
        }
    }

    private void FaceChaseDirection(Vector3 target_if_visible, Vector3 target_if_not)
    {
        if (can_see_player && player != null)
            FaceTarget(target_if_visible);
        else if (guard.velocity.sqrMagnitude > 0.01f)
            FaceMovementDirection();
        else
            FaceTarget(target_if_not);
    }

    private void Investigate()
    {
        SetGuardDestination(player_last_position);
        FaceChaseDirection(player.position, player_last_position);

        investigate_timer += Time.deltaTime;

        bool reached = HasReachedDestination() &&
            Vector3.Distance(current_destination, player_last_position) <= 0.5f;

        if ((reached || investigate_timer >= investigate_timeout) && active_routine == null)
        {
            is_investigating = false;
            investigate_timer = 0f;

            if (sight_loss_direction.sqrMagnitude < 0.01f)
                sight_loss_direction = player_last_direction.sqrMagnitude > 0.01f
                    ? player_last_direction
                    : transform.forward;

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
            Vector3 to_target = (target_point.position - transform.position).normalized;
            bool facing_target = Quaternion.Angle(
                transform.rotation, YawFromDirection(to_target)) < 1f;

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
            target_point = (target_point == point_a) ? point_b : point_a;
        }
    }
}