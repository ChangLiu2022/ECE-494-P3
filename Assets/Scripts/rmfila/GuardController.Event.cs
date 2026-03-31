using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;

public partial class GuardController
{
    private void OnAlertEvent(AlertEvent e)
    {
        investigate_timer = 0f;
        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;
        player_last_position = e.position + new Vector3(rand_offset.x, 0f, rand_offset.y);
        is_investigating = true;
        is_spotting = false;
        current_chase_bar = chase_bar_max;
        CancelAllRoutines();

        if (current_tier < GuardTier.Tier3)
            TierUp(GuardTier.Tier3);
    }

    private void OnNoiseWaveEvent(NoiseWaveEvent e)
    {
        float distance_unfiltered = Vector3.Distance(transform.position, e.origin);
        if (distance_unfiltered > e.radius)
            return;

        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(e.origin, transform.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathInvalid ||
            path.status == NavMeshPathStatus.PathPartial)
            return;

        float total_path = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            total_path += Vector3.Distance(path.corners[i - 1], path.corners[i]);

        if (total_path > e.radius)
            return;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 direction = (path.corners[i] - path.corners[i - 1]).normalized;
            float seg_dist = Vector3.Distance(path.corners[i - 1], path.corners[i]);
            if (Physics.Raycast(path.corners[i - 1], direction, seg_dist,
                door_mask, QueryTriggerInteraction.Ignore))
                return;
        }

        float delay = total_path / noise_wave_expand_speed;
        StartCoroutine(ReactToNoiseWave(e.is_gunshot, e.origin, delay));
    }

    private void OnErraticAlertEvent(ErraticAlertEvent e)
    {
        if (current_tier >= GuardTier.Tier2)
            return;

        current_tier = GuardTier.Tier2;
        ApplySpeed();

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

        if (!collision.CompareTag("Body"))
            return;

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

        if (!is_catching)
            StartCoroutine(GracePeriodRoutine());
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Body") && !is_catching &&
            current_tier >= GuardTier.Tier3)
            EndGame();
    }

    private IEnumerator GracePeriodRoutine()
    {
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        is_catching = false;
    }

    private IEnumerator ReactToNoiseWave(bool is_gunshot, Vector3 origin, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);

        investigate_timer = 0f;
        CancelAllRoutines();

        Vector2 rand_offset = Random.insideUnitCircle * 0.75f;
        player_last_position = origin + new Vector3(rand_offset.x, 0f, rand_offset.y);

        if (is_gunshot)
        {
            guns_out = true;
            TierUp(GuardTier.Tier4);
            is_investigating = true;
            is_spotting = false;
        }
        else
        {
            if (current_tier < GuardTier.Tier3)
            {
                // FIX: the original set is_spotting = true here, which called
                // FaceTarget(player.position) through walls every frame and froze
                // the guard uselessly. now the guard just goes to investigate the
                // noise source directly - much more sensible and scarier behavior.
                is_investigating = true;
                is_spotting = false;

                // partially fill the chase bar to show guard is on high alert
                current_chase_bar = Mathf.Max(current_chase_bar, chase_bar_max * 0.5f);

                // bump to tier 2 if still tier 1 so they move at alert speed
                if (current_tier < GuardTier.Tier2)
                {
                    current_tier = GuardTier.Tier2;
                    ApplySpeed();
                }
            }
            else
            {
                // already chasing - go check the sound's source
                is_investigating = true;
                is_spotting = false;
                current_chase_bar = chase_bar_max;
            }
        }
    }
}