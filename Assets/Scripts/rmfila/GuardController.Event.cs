using System.Collections;
using UnityEngine;
using static GameEvents;


public partial class GuardController
{
    // overrides player_last_position if called
    private void OnAlertEvent(AlertEvent e)
    {
        if (player != null)
        {
            Vector2 rand_offset = Random.insideUnitCircle * 0.75f;
            player_last_position = player.position + 
                new Vector3(rand_offset.x, 0f, rand_offset.y);
        }

        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        CancelSearchAndReturn();

        if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);
    }


    // overrides player_last_position if called
    private void OnGunshotEvent(GunshotEvent e)
    {
        if (Vector3.Distance
            (transform.position, e.player_position) > gunshot_alert_radius)
        {
            return;
        }

        // move to where the player was when they fired
        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;
        player_last_position = e.player_position + 
            new Vector3(rand_offset.x, 0f, rand_offset.y);

        guns_out = true;
        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;

        CancelSearchAndReturn();
        TierUp(GuardTier.Tier4);
    }


    // another guard returned home after a failed chase.
    // every guard that isn't already chasing drops to tier 2
    private void OnErraticAlertEvent(ErraticAlertEvent e)
    {
        if (current_tier >= GuardTier.Tier3)
            return;

        if (current_tier == GuardTier.Tier2)
            return;

        current_tier = GuardTier.Tier2;
        ApplySpeed();

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

        if (collision.CompareTag("Body") == false)
            return;

        // already chasing = instant game over
        if (current_tier >= GuardTier.Tier3)
        {
            EndGame();
            return;
        }

        if (player != null)
            player_last_position = player.position;

        is_spotting = false;
        current_chase_bar = chase_bar_max;

        TierUp(GuardTier.Tier3);

        if (is_catching == false)
            StartCoroutine(GracePeriodRoutine());
    }


    private void OnTriggerStay(Collider collision)
    {
        // still touching after grace period expired = game over
        if (collision.CompareTag("Body") && is_catching == false &&
            current_tier >= GuardTier.Tier3)
            EndGame();
    }

    // gives player that touched guard a moment of time
    private IEnumerator GracePeriodRoutine()
    {
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        is_catching = false;
    }
}
