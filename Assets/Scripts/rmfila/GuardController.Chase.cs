using UnityEngine;


public partial class GuardController
{
    // handles the freeze-and-fill phase before a chase begins
    private void HandleSpotting()
    {
        // only reset path if not in active routine
        // (searching, returning, OR already in erratic scan)
        if (is_searching == false && 
            is_returning == false && 
            static_search_routine == null)
        {
            guard.ResetPath();
        }

        // always face the player when spotting them
        // it looks like the guard is going "hmmm what is that"
        FaceTarget(player.position);

        // player is in sight/in vision cone
        // fill the bar and cache the player's
        // position and direction for the chase 
        if (can_see_player == true && player != null)
        {
            // save the latest information of the player
            UpdatePlayerTracking();

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
            // no longer spotting but activating a chase
            if (current_chase_bar >= chase_bar_max)
            {
                is_spotting = false;

                if (guns_out)
                    TierUp(GuardTier.Tier4);

                else
                    TierUp(GuardTier.Tier3);
            }
        }

        // player left the cone, decay the bar back to zero
        // no longer spotting, but returning to normal,
        else
        {
            current_chase_bar = Mathf.Max(current_chase_bar - 
                chase_bar_decay * Time.fixedDeltaTime, 0f);

            if (current_chase_bar <= 0f)
            {
                is_spotting = false;

                // go back to whatever you were doing before spotting the
                // player
                if (active_routine == null)
                {
                    static_search_routine = StartCoroutine(ResumeIdleRoutine());
                }
            }
        }
    }


    // moves guard towards last known position
    private void ChasePlayer()
    {
        // if can see player, always chase directly
        if (can_see_player)
            SetGuardDestination(player.position);

        // otherwise, chase to the predicted position, if not there already
        else
            SetGuardDestination(player_last_position);

        FaceChaseDirection(player.position, player_last_position);

        // reached last known position with no sight, begin search.
        // don't start until the pursuit window has closed
        if (can_see_player == false && sight_loss_timer <= 0f &&
            HasReachedDestination() && active_routine == null)
        {
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    // tracks position + direction while guard has sight, freezes on loss
    // player_last_position only updates here if the guard can see the player
    private void UpdateChaseTracking()
    {
        if (can_see_player && player != null)
        {
            // save the latest information of the player to
            // use when we lose sight reset timer to full
            sight_loss_timer = pursuit_window;
            UpdatePlayerTracking();

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

        // capture the point to later use for the point to overshoot
        if (had_sight_last_frame && can_see_player == false)
            sight_loss_direction =
                (player_last_position - transform.position).normalized;

        had_sight_last_frame = can_see_player;
    }


    private void UpdatePlayerTracking()
    {
        // when able to see the player, cache its last known poistion
        // and direction
        player_last_position = player.position;

        // calculates where the player is now and where they were last
        // frame
        Vector3 difference = player.position - player_previous_position;

        // only update the direction if the change is substantial enough
        if (difference.sqrMagnitude > 0.01f)
        {
            player_last_direction = difference.normalized;
            // calculate speed for pursuit prediction on sight loss
            player_estimated_speed = difference.magnitude / Time.fixedDeltaTime;
        }

        player_previous_position = player.position;
    }


    // can only move up, ErraticDrop is the way to downgrade
    private void TierUp(GuardTier tier)
    {
        // cannot downgrade
        if (tier <= current_tier)
            return;

        // tier up, set chase bar to full so guard chases player
        // and reset the sight loss timer so they dont lose the player
        // right away after tiering up
        current_tier = tier;
        current_chase_bar = chase_bar_max;
        sight_loss_timer = pursuit_window;

        // if we are running a static search rotuine, stop it
        if (static_search_routine != null)
        {
            StopCoroutine(static_search_routine);
            static_search_routine = null;
        }

        // if we heard a gunshot or were shot and are going to tier 4
        // draw weapon after a delay, and increase speed/aggression
        if (tier == GuardTier.Tier4)
        {
            guns_out = true;

            if (is_drawing_weapon == false)
                StartCoroutine(DrawWeaponRoutine());
        }

        ApplySpeed();
    }


    // this is how we downgrade, only ever going down to tier 2 erratic mode
    private void ErraticDrop()
    {
        // if we called it, it means we are only going to tier 2
        current_tier = GuardTier.Tier2;
        // reset chase bar, we arent chasing anymore because this is called
        // after everything
        current_chase_bar = 0f;
        // since we set current_tier to Tier2, the guard will switch to 
        // erratic speed always. Once we leave tier 1 patrol speed, we never
        // go back to that slow of a speed again.
        ApplySpeed();

        if (guard_mode != GuardMode.Patrol)
        {
            if (static_search_routine != null)
                StopCoroutine(static_search_routine);

            // static and static search guards now pan back and forth in
            // their starting locations after returning from a chase
            static_search_routine = StartCoroutine(StaticScanRoutine());
        }

        // alert every other guard in the building to go erratic
        // this is essentially like a walky talky system. Like hey, I for
        // sure just saw some thief, so everyone be on alert, we lost visual
        EventBus.Publish(new GameEvents.ErraticAlertEvent());
    }


    private void ApplySpeed()
    {
        // only on start is this applied
        if (current_tier == GuardTier.Tier1)
            guard.speed = patrol_speed;

        // after any chase occurs, we never go back
        // to patrol speed, except for SearchAndReturn
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


    // walks to the player's last known position
    // and trys to spot where they last were
    private void Investigate()
    {
        // walk to last known player position
        SetGuardDestination(player_last_position);

        // look at the player if visible, or where the player went
        FaceChaseDirection(player.position, player_last_position);

        // count how long we've been investigating for
        investigate_timer += Time.deltaTime;


        bool reached_actual_target = HasReachedDestination() &&
            Vector3.Distance
            (current_destination, player_last_position) <= 0.3f;

        // stop investigating if we've reached the spot, or we
        // investigated for too long
        if ((reached_actual_target || investigate_timer >=
            investigate_timeout) && active_routine == null)
        {
            is_investigating = false;

            // figure ouut what way we are facing when we lost the player
            if (sight_loss_direction.sqrMagnitude < 0.01f)
            {
                if (last_chase_velocity.sqrMagnitude > 0.01f)
                    sight_loss_direction = last_chase_velocity;

                else
                    sight_loss_direction = transform.forward;
            }

            // start searching for them based on that direction
            active_routine = StartCoroutine(SearchAndReturnRoutine());
        }
    }


    private void Patrol()
    {
        // do not have a target, dont do anything,
        // something is wrong here, shouldnt be patrol w/o target
        if (target_point == null)
            return;

        // take a moment to turn around and face the other target point to start moving
        if (is_paused)
        {
            FaceTarget(target_point.position);
            pause_time -= Time.fixedDeltaTime;

            Vector3 to_target =
                (target_point.position - transform.position).normalized;

            bool facing_target =
                Quaternion.Angle(transform.rotation,
                YawFromDirection(to_target)) < 1f;

            // check to make our pause time is up, and 
            // we are facing the next target point before moving on
            if (pause_time <= 0f && facing_target)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }

            return;
        }

        // face the direction we are moving
        FaceMovementDirection();

        // once we hit out destination, pause, and swap targets
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
