using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;


public partial class GuardController
{
    // overrides player_last_position if called
    private void OnAlertEvent(AlertEvent e)
    {
        // reset timer when the alert is called
        investigate_timer = 0f;

        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;

        player_last_position = 
            e.position + new Vector3(rand_offset.x, 0f, rand_offset.y);

        // we want to investigate the laser alert
        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        // cancel anything going on
        CancelAllRoutines();

        // alert is triggered by lasers going off,
        // nothing should indicate to use lethality yet
        // do not override tier4, as once a guard pulls
        // their guns out, they stay out
        if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);
    }


    private void OnNoiseWaveEvent(NoiseWaveEvent e)
    {
        // if the guard is farther than the sound radius
        // ignore it immediately
        float distance_unfiltered = 
            Vector3.Distance(transform.position, e.origin);

        if (distance_unfiltered > e.radius)
            return;

        // check if sound can actually reach this guard via walkable path
        NavMeshPath path = new NavMeshPath();

        // navmesh path is made up of waypoints
        NavMesh.CalculatePath(e.origin, 
            transform.position, 
            NavMesh.AllAreas, 
            path);

        // if path is not valid, do nothing
        if (path.status == NavMeshPathStatus.PathInvalid ||
            path.status == NavMeshPathStatus.PathPartial)
            return;

        float total_path = 0f;

        // each iteration measures the distance between consecutive corners
        // corners are from starting point, to around corners, to destination
        // get the total distance the path is to travel between all corners
        for (int i = 1; i < path.corners.Length; i++)
            total_path += 
                Vector3.Distance(path.corners[i - 1], path.corners[i]);

        // if the total path length is too far from the radius to be heard
        // ignore it. This means that the distance the sound has to travel
        // is the actual walkable path, not a straight line distance
        if (total_path > e.radius)
            return;

        // checks along each segment of the path for door colliders
        // QueryTriggerInteraction.Ignore = doors are triggers
        // but we want to hit them
        for (int i = 1; i < path.corners.Length; i++)
        {

            Vector3 direction = 
                (path.corners[i] - path.corners[i - 1]).normalized;

            float distance_filtered = 
                Vector3.Distance(path.corners[i - 1], path.corners[i]);

            // if this path segment hits a door, sound can't reach through it
            if (Physics.Raycast(path.corners[i - 1],
                direction,
                distance_filtered, 
                door_mask, 
                QueryTriggerInteraction.Ignore))
            {
                return;
            }
        }

        // delay based on path length so closer guards react sooner
        float delay = total_path / noise_wave_expand_speed;

        StartCoroutine(ReactToNoiseWave(e.is_gunshot, e.origin, delay));
    }


    // another guard returned home after a failed chase.
    // every guard that isn't already chasing changes to tier 2
    private void OnErraticAlertEvent(ErraticAlertEvent e)
    {
        // means guard is already tier 2 or chasing tier
        if (current_tier >= GuardTier.Tier2)
            return;

        // only change if guard it tier 1
        current_tier = GuardTier.Tier2;
        ApplySpeed();

        // regardless of if the guard is static or static searching
        // they will not be treated the same meaning all static guards
        // become static searching guards
        if (guard_mode != GuardMode.Patrol)
        {
            if (static_search_routine != null)
                StopCoroutine(static_search_routine);

            static_search_routine = StartCoroutine(StaticScanRoutine());
        }
    }


    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            TakeDamage(collision);
            return;
        }

        // anything else besides the player's body with the sphere collider
        // on it and the tag "Body", is just ignored
        if (collision.CompareTag("Body") == false)
            return;

        // if the guard was already chasing the player when the trigger
        // happened, it means that we already gave them a grace period
        // and now its fairgame
        if (current_tier >= GuardTier.Tier3)
        {
            EndGame();
            return;
        }

        // we known where player is because they bumped us, or the guard
        if (player != null)
            player_last_position = player.position;

        is_spotting = false;
        current_chase_bar = chase_bar_max;

        TierUp(GuardTier.Tier3);

        // is_catching is used for the graceperiod, so after a grace
        // period is given, it updates is_catching to be false
        if (is_catching == false)
            StartCoroutine(GracePeriodRoutine());
    }


    private void OnTriggerStay(Collider collision)
    {
        // still touching after grace period expired = game over
        if (collision.CompareTag("Body") && is_catching == false &&
            current_tier >= GuardTier.Tier3)
        {
            EndGame();
        }
    }

    // gives player that touched guard a moment of time to get away
    // without instantly getting a game over
    private IEnumerator GracePeriodRoutine()
    {
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        is_catching = false;
    }


    private IEnumerator ReactToNoiseWave
        (bool is_gunshot, Vector3 origin, float delay)
    {
        // do not react to anything until the delay based on distance
        // is waited out
        yield return new WaitForSeconds(delay + 0.1f);

        // no investigation should be happening
        // guard is now reacting to noise
        investigate_timer = 0f;

        CancelAllRoutines();

        // go to noise but choose a random offset around the source
        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;

        player_last_position = 
            origin + new Vector3(rand_offset.x, 0f, rand_offset.y);

        // if the noise was a gunshot, guard draws gun out
        if (is_gunshot)
        {
            guns_out = true;
            TierUp(GuardTier.Tier4);
            is_investigating = true;
            is_spotting = false;
        }

        else
        {
            // guard is not yet chasing
            if (current_tier < GuardTier.Tier3)
            {
                // this means guard heard player's footsteps, so start
                // spotting, freeze, stare, and let bar fill up
                current_chase_bar = Mathf.Max(current_chase_bar, chase_bar_fill_rate);
                is_spotting = true;
                is_investigating = false;
                guard.ResetPath();
            }

            // guard is already chasing
            else
            {
                // already alert so skip spotting and go to investigate
                // the sound's origin
                is_investigating = true;
                is_spotting = false;
                current_chase_bar = chase_bar_max;
            }
        }
    }
}
