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


    // determines if the guard's agent has reached its destination
    // or if its close enough.
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

        // sight_loss_direction is the direction the guard was
        // moving to the player when it loss sight. Ensure its not 0/garbage
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            if (TryGetNavPoint(player_last_position, sight_loss_direction, 
                overshoot_distance, 0.5f, out Vector3 overshoot))
            {
                // stop a bunch of guards from getting stuck
                // while searching
                yield return MoveWithTimeout(overshoot, 3f);
            }
        }

        // anchor the current rotation to base all our rotations on
        Quaternion scan_anchor = transform.rotation;

        yield return ScanIfOpen(scan_anchor, -70f);
        yield return new WaitForSeconds(0.4f);
        yield return ScanIfOpen(scan_anchor, 70f);
        yield return new WaitForSeconds(0.4f);
        yield return ScanIfOpen(scan_anchor, 0f, 1f);

        // continue further in the same direction,
        // looks around the corner more,
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            guard.speed = patrol_speed;

            if (TryGetNavPoint(transform.position, sight_loss_direction, 
                overshoot_distance * 0.6f, 0.3f, out Vector3 further))
            {
                yield return MoveWithTimeout(further, 3f);
            }
        }

        scan_anchor = transform.rotation;

        yield return ScanIfOpen(scan_anchor, -45f);
        yield return new WaitForSeconds(0.3f);
        yield return ScanIfOpen(scan_anchor, 45f);
        yield return new WaitForSeconds(0.3f);
        yield return ScanIfOpen(scan_anchor, 0f, 1f);

        current_chase_bar = 0f;
        is_searching = false;
        is_returning = true;

        ErraticDrop();

        yield return MoveWithTimeout(start_position, return_timeout);

        // if guard is patrol, go back to patrolling
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


    private IEnumerator MoveWithTimeout(Vector3 destination, float timeout)
    {
        guard.SetDestination(destination);

        float elapsed = 0f;

        // wait until we have actuall arrived to the desintation
        // to move on
        while (elapsed < timeout && !HasReachedDestination())
        {
            elapsed += Time.deltaTime;
            // until we reach the dest, keep updating the facing
            // direction of guard to be correct
            FaceMovementDirection();
            yield return null;
        }

        guard.ResetPath();
    }


    private bool TryGetNavPoint(Vector3 origin, Vector3 direction,
        float distance, float pullback, out Vector3 result)
    {
        // ----- TODO: the guard shouldn't stop 0.5f units away from -----
        //       the edge hit, otherwise, they do not make it to
        //       their ideal path, but stop at the wall they hit

        // this is the raw point we are going to be traveling to when
        // the guard loses sight of the player.
        // This is the "ideal" destination
        Vector3 raw = origin + direction * distance;
        Vector3 point;

        // Raycast to find if the ideal overshoot point is in a way
        if (NavMesh.Raycast
            (origin, raw, out NavMeshHit edge_hit, NavMesh.AllAreas))
        {             
            // if we get an edge hit, pull the distance of where the ray
            // hit back by 0.5f, stop guard from getting stuuck
            point = edge_hit.position - direction * pullback;
        }

        else
            // otherwise, the idea spot is ideal
            point = raw;

        // safety net to ensure that the position
        // we got is a valid position
        if (NavMesh.SamplePosition
            (point, out NavMeshHit hit, distance, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }


    private IEnumerator ResumeIdleRoutine()
    {
        yield return RotateUntilFacing(start_rotation);

        if (guard_mode == GuardMode.StaticSearch)
            static_search_routine = StartCoroutine(StaticScanRoutine());

        else if (guard_mode == GuardMode.Patrol && 
            point_a != null && point_b != null)
        {
            target_point = point_a;
            guard.SetDestination(target_point.position);
        }

        static_search_routine = null;
    }
}