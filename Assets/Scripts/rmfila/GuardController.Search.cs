using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class GuardController
{
    private void CancelSearchAndReturn()
    {
        if (active_routine != null)
        {
            StopCoroutine(active_routine);
            active_routine = null;
        }

        if (static_search_routine != null)
        {
            StopCoroutine(static_search_routine);
            static_search_routine = null;
        }

        is_searching = false;
        is_returning = false;
        ApplySpeed();
    }


    private bool HasReachedDestination()
    {
        return guard.pathPending == false &&
               guard.remainingDistance <= guard.stoppingDistance + 0.1f;
    }


    // ChasePlayer brought us to player_last_position.
    // run past in the direction the player was traveling,
    // scan left/right anchored to the stopping position, then return home
    private IEnumerator SearchAndReturnRoutine()
    {
        is_searching = true;
        guard.ResetPath();

        // push past the last known position in the direction the guard
        // was already traveling, continues naturally around corners
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            Vector3 overshoot_raw = player_last_position + 
                sight_loss_direction * overshoot_distance;

            Vector3 overshoot_point;

            if (NavMesh.Raycast(player_last_position,
                overshoot_raw, out NavMeshHit edge_hit, NavMesh.AllAreas))
            {
                overshoot_point = 
                    edge_hit.position - sight_loss_direction * 0.3f;
            }
            else
                overshoot_point = overshoot_raw;

            if (NavMesh.SamplePosition(overshoot_point, out NavMeshHit hit,
                overshoot_distance, NavMesh.AllAreas))
            {
                guard.SetDestination(hit.position);

                while (HasReachedDestination() == false)
                {
                    FaceMovementDirection();
                    yield return null;
                }
            }
        }

        guard.ResetPath();

        Quaternion scan_anchor = transform.rotation;

        guard.obstacleAvoidanceType = 
            ObstacleAvoidanceType.NoObstacleAvoidance;

        yield return ScanIfOpen(scan_anchor, -70f);
        yield return new WaitForSeconds(0.4f);
        yield return ScanIfOpen(scan_anchor, 70f);
        yield return new WaitForSeconds(0.4f);
        yield return ScanIfOpen(scan_anchor, 0f, 1f);

        guard.obstacleAvoidanceType = 
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // continue further in the same direction,
        // looks around the corner more,
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            Vector3 further_raw = transform.position + sight_loss_direction *
                (overshoot_distance * 0.6f);

            Vector3 further_point;

            if (NavMesh.Raycast(transform.position,
                further_raw, out NavMeshHit edge_hit2, NavMesh.AllAreas))
            {
                further_point =
                    edge_hit2.position - sight_loss_direction * 0.3f;
            }

            else
                further_point = further_raw;

            if (NavMesh.SamplePosition(further_point, out NavMeshHit hit2,
                overshoot_distance, NavMesh.AllAreas))
            {
                guard.speed = patrol_speed;
                guard.SetDestination(hit2.position);

                float push_elapsed = 0f;

                while (push_elapsed < 3f && HasReachedDestination() == false)
                {
                    push_elapsed += Time.deltaTime;
                    FaceMovementDirection();
                    yield return null;
                }
            }
        }

        guard.ResetPath();

        scan_anchor = transform.rotation;

        guard.obstacleAvoidanceType = 
            ObstacleAvoidanceType.NoObstacleAvoidance;

        yield return ScanIfOpen(scan_anchor, -45f);
        yield return new WaitForSeconds(0.3f);
        yield return ScanIfOpen(scan_anchor, 45f);
        yield return new WaitForSeconds(0.3f);
        yield return ScanIfOpen(scan_anchor, 0f, 1f);

        guard.obstacleAvoidanceType = 
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        current_chase_bar = 0f;
        is_searching = false;
        is_returning = true;

        ErraticDrop();

        guard.SetDestination(start_position);

        float time_elapsed = 0f;

        while (time_elapsed < return_timeout &&
            HasReachedDestination() == false)
        {
            time_elapsed += Time.deltaTime;
            FaceMovementDirection();
            yield return null;
        }

        if (guard_mode == GuardMode.Patrol && 
            point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.SetDestination(target_point.position);
        }

        is_returning = false;
        active_routine = null;
    }


    // static guards scan back and forth after dropping to tier 2
    private IEnumerator StaticScanRoutine()
    {
        while (true)
        {
            Quaternion look_right = 
                start_rotation * Quaternion.Euler(0f, 180f, 0f);
            yield return RotateUntilFacing(look_right);
            yield return new WaitForSeconds(pause_duration);
            yield return RotateUntilFacing(start_rotation);
            yield return new WaitForSeconds(pause_duration);
        }
    }


    private IEnumerator RotateUntilFacing(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target,
                turn_speed * 1.5f * Time.deltaTime);
            yield return null;
        }
    }


    private bool WallWithin(Quaternion target_rotation, float distance)
    {
        Vector3 dir = target_rotation * Vector3.forward;
        return Physics.Raycast(transform.position, dir, distance, wall_mask);
    }


    private IEnumerator ScanIfOpen
        (Quaternion anchor, float degrees, float check_dist = 3f)
    {
        Quaternion target = anchor * Quaternion.Euler(0f, degrees, 0f);

        if (WallWithin(target, check_dist))
            yield break;

        yield return ScanToAngle(anchor, degrees);
    }


    // smoothly rotates to a yaw offset from a fixed world anchor
    private IEnumerator ScanToAngle(Quaternion anchor, float degrees)
    {
        Quaternion target = anchor * Quaternion.Euler(0f, degrees, 0f);

        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turn_speed * Time.deltaTime);
            yield return null;
        }
    }
}
