using System.Collections;
using UnityEngine;
using static GameEvents;


public partial class GuardController
{
    // overrides player_last_position if called
    private void OnAlertEvent(AlertEvent e)
    {
        // reset timer when the alert is called
        investigate_timer = 0f;

        if (player != null)
        {
            // so not all guards have the same target position
            // set when going to an alert
            Vector2 rand_offset = Random.insideUnitCircle * 0.75f;

            // update the last known location of the player on the  alert
            // with the random offset added
            player_last_position = player.position + 
                new Vector3(rand_offset.x, 0f, rand_offset.y);
        }

        // we want to investigate the laser alert
        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        // cancel anything going on
        CancelSearchAndReturn();

        // alert is triggered by lasers going off,
        // nothing should indicate to use lethality yet
        // do not override tier4, as once a guard pulls
        // their guns out, they stay out
        if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);
    }


    // overrides player_last_position if called
    private void OnGunshotEvent(GunshotEvent e)
    {
        investigate_timer = 0f;

        // check that the distance from the current guard to the location
        // where the gunshot was produced is within the range of hearing
        if (Vector3.Distance
            (transform.position, e.player_position) > gunshot_alert_radius)
        {
            // return if its out of range
            return;
        }

        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;

        player_last_position = e.player_position + 
            new Vector3(rand_offset.x, 0f, rand_offset.y);

        // we are lethal now
        guns_out = true;
        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        CancelSearchAndReturn();
        TierUp(GuardTier.Tier4);
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
        // they will not be treated the same -- meaning all static guards
        // become static searching guards
        if (guard_mode != GuardMode.Patrol)
            static_search_routine = StartCoroutine(StaticScanRoutine());
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
}
