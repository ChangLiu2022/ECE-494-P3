using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class GuardController
{
    private void CancelAllRoutines()
    {
        // a chase failure sequence is happening
        // stop it
        if (active_routine != null)
        {
            StopCoroutine(active_routine);
            active_routine = null;
        }

        // a static search sequence is happening
        // stop it
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
        // this means guard cant find a complete path, guard is stuck
        // or destination is unreachable
        if (guard.pathStatus == NavMeshPathStatus.PathPartial)
            return false;

        // otherwise, if the path has finished calculating
        // and the guard is close enough to the destination, we have arrived
        return guard.pathPending == false &&
               guard.remainingDistance <= guard.stoppingDistance + 0.1f;
    }


    // guard reached last known player position
    // overshoot in the direction they were
    // heading (sight_loss_direction), scan around,
    // then return to start position
    private IEnumerator SearchAndReturnRoutine()
    {
        is_searching = true;
        guard.ResetPath();

        // sight_loss_direction is the direction the guard was
        // moving to the player when it loss sight
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            // get a new point on the navmesh to walk to based
            // on the overshoot distance and sight_loss_direction
            if (TryGetNavPathPoint(sight_loss_direction, 
                overshoot_distance, out Vector3 overshoot))
            {
                // stop a bunch of guards from getting stuck
                // while searching with timeout
                yield return MoveWithTimeout(overshoot, 3f);
            }
        }

        // first scan
        // wide sweep -70 to +70
        yield return ScanPattern(transform.rotation, 70f, 70f, 0.4f);

        // continue further in the same direction,
        // looks around the corner more,
        if (sight_loss_direction.sqrMagnitude > 0.01f)
        {
            // turn the speed down so the guard walks more carefully
            guard.speed = patrol_speed;

            // get a new overshoot point further from out current position,
            // in the same direction, but slightly less far than the original
            // overshoot distance. Purely aesthetic choice here
            if (TryGetNavPathPoint(sight_loss_direction, 
                overshoot_distance * 0.6f, out Vector3 further))
            {
                yield return MoveWithTimeout(further, 3f);
            }
        }

        // second scan
        // small sweep -45 to +45
        yield return ScanPattern(transform.rotation, 45f, 45f, 0.3f);

        current_chase_bar = 0f;
        is_searching = false;
        is_returning = true;

        // we didn't didn't find anything, we drop down to tier 2 and
        // return back to the guard's starting position
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
        Quaternion look_left = start_rotation * Quaternion.Euler(0f, -180f, 0f);
        Quaternion look_right = start_rotation * Quaternion.Euler(0f, 0, 0f);

        while (true)
        {
            yield return RotateTowards(look_left, 1.5f);
            yield return new WaitForSeconds(pause_duration);
            yield return RotateTowards(look_right, 1.5f);
            yield return new WaitForSeconds(pause_duration);
        }
    }


    // smoothly rotate to face a target
    // speed multiplier allows faster/slower rotation
    // wall check is optional for scanning phases
    private IEnumerator RotateTowards
        (Quaternion target, 
        float speed_multiplier = 1f, 
        float wall_check_dist = 0f)
    {
        // checking for walls, skip rotation if blocked
        if (wall_check_dist > 0f && WallWithin(target, wall_check_dist))
            yield break;

        while (Quaternion.Angle(transform.rotation, target) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target,
                turn_speed * speed_multiplier * Time.deltaTime);

            yield return null;
        }
    }


    private bool WallWithin(Quaternion target_rotation, float distance)
    {
        // the direction we need to turn to face the target
        Vector3 direction = target_rotation * Vector3.forward;

        // returns true if the raycast hits a wall from the
        // guard's position in the direction of the target rotation
        return Physics.Raycast
            (transform.position, direction, distance, wall_mask);
    }


    // scan a sweep pattern
    // left angle, wait, right angle, wait, back to center
    private IEnumerator ScanPattern
        (Quaternion anchor, 
        float left_angle, 
        float right_angle, 
        float wait_time)
    {
        Quaternion left_target = anchor * Quaternion.Euler(0f, -left_angle, 0f);
        Quaternion right_target = anchor * Quaternion.Euler(0f, right_angle, 0f);
        Quaternion center_target = anchor;

        yield return RotateTowards(left_target, 1f, 3f);
        yield return new WaitForSeconds(wait_time);
        yield return RotateTowards(right_target, 1f, 3f);
        yield return new WaitForSeconds(wait_time);
        yield return RotateTowards(center_target, 1f, 3f);
    }


    private IEnumerator MoveWithTimeout(Vector3 destination, float timeout)
    {
        guard.SetDestination(destination);

        float timeout_time = 0f;
        float stuck_timer = 0f;
        Vector3 last_pos = transform.position;

        while (timeout_time < timeout && !HasReachedDestination())
        {
            timeout_time += Time.deltaTime;
            FaceMovementDirection();

            // if guard hasn't moved 0.1 units in 1.5s, consider arrived.
            // handles guards blocked by closed doors or geometry.
            if (Vector3.Distance(transform.position, last_pos) > 0.1f)
            {
                last_pos = transform.position;
                stuck_timer = 0f;
            }
            else
            {
                stuck_timer += Time.deltaTime;
                if (stuck_timer >= 1.5f) break;
            }

            yield return null;
        }

        guard.ResetPath();
    }


    // Routes the overshoot point along the actual NavMesh path rather than a
    // straight line. This means the guard naturally rounds corners to check
    // where the player fled, instead of walking into a wall and stopping.
    private bool TryGetNavPathPoint(Vector3 direction, float distance, out Vector3 result)
    {
        // project a far target in the pursuit direction, then let NavMesh route to it
        Vector3 far_target = transform.position + direction * (distance * 3f);

        // snap the far target to the navmesh surface
        if (!NavMesh.SamplePosition(far_target, out NavMeshHit far_hit, distance * 3f, NavMesh.AllAreas))
        {
            result = Vector3.zero;
            return false;
        }

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(transform.position, far_hit.position, NavMesh.AllAreas, path)
            || path.status == NavMeshPathStatus.PathInvalid
            || path.corners.Length < 2)
        {
            result = Vector3.zero;
            return false;
        }

        // walk the path corners until we've accumulated ~distance worth of travel
        // this gives a point that is 'distance' units away along the real walkable path
        float accumulated = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            float seg = Vector3.Distance(path.corners[i - 1], path.corners[i]);

            if (accumulated + seg >= distance)
            {
                float t = (distance - accumulated) / seg;
                result = Vector3.Lerp(path.corners[i - 1], path.corners[i], t);
                return true;
            }

            accumulated += seg;
        }

        // path ended before reaching distance — use the final corner
        result = path.corners[path.corners.Length - 1];
        return true;
    }


    private IEnumerator ResumeIdleRoutine()
    {
        // already in starting position, not rotate back to
        // starting rotation
        yield return RotateTowards(start_rotation, 1.5f);

        if (guard_mode == GuardMode.StaticSearch ||
            (current_tier == GuardTier.Tier2 && 
            guard_mode == GuardMode.Patrol))
        {
            // if not already acively static searching, start the routine
            // otherwise, stop then start again
            if (static_search_routine != null)
                StopCoroutine(static_search_routine);

            static_search_routine = StartCoroutine(StaticScanRoutine());
        }

        else
        {
            // otherwise we are not a static search guard
            // so just do nothing for a static guard, or return
            // to patrolling for a patrol guard
            static_search_routine = null;

            if (guard_mode == GuardMode.Patrol && 
                point_a != null && point_b != null)
            {
                target_point = point_a;
                guard.SetDestination(target_point.position);
            }
        }
    }
}