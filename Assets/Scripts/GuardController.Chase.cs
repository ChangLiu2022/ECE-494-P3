using System.Collections;
using UnityEngine;

public partial class GuardController
{
    // guard moves to the player's last position and faces the player when
    // they can see them. the guard can lose interest in the player
    // if they somehow make it super far away
    private void ChasePlayer()
    {
        SetGuardDestination(player_last_position);

        if (can_see_player && player != null) 
            FaceTarget(player.position);

        else if (guard.velocity.sqrMagnitude > 0.01f) 
            FaceMovementDirection();

        else FaceTarget(player_last_position);

        if (HasReachedDestination() == true && active_routine == null
            && chase_timer >= min_chase_duration)
        {
            if (player != null && Vector3.Distance(
                transform.position, player.position) > give_up_distance)
            {
                active_routine = StartCoroutine(ReturnToStartRoutine());
            }
        }
    }


    // this is what is called when the player is somehow able to run
    // far enough away. legit should not run, but I already had the code
    // from when guards were smarter, so its staying
    private IEnumerator ReturnToStartRoutine()
    {
        stuck_timer = 0f;
        is_returning = true;
        is_alerted = false;
        can_see_player = false;

        guard.SetDestination(start_position);

        float timer = 0f;

        // keep walking home until either the return timeout time is reached
        // or we actually make it to the starting position
        while (timer < return_timeout && HasReachedDestination() == false)
        {
            timer += Time.deltaTime;
            FaceMovementDirection();
            yield return null;
        }

        // once we get back to the starting position, reset the path
        // and reset the rotation
        guard.ResetPath();
        transform.rotation = start_rotation;

        is_returning = false;

        // go back to patrolling if guard was a patrol guard
        if (guard_mode == GuardMode.Patrol
            && point_a != null && point_b != null)
        {
            target_point = point_a;
            active_routine = StartCoroutine(PatrolRoutine());
        }

        else
        {
            if (guard_mode == GuardMode.StaticSearch)
                static_scan_routine = StartCoroutine(StaticScanRoutine());

            active_routine = null;
        }
    }


    // static search guards turn based on openness. this means
    // guards will rotate where ever there is more open space
    // and less walls that conflict with vision.
    private IEnumerator StaticScanRoutine()
    {
        float start_direction = start_rotation.eulerAngles.y;
        float opposite_direction = start_direction + 180f;

        while (true)
        {
            // to the right of the guard's starting direction
            // get the "score" it got for how open it is
            float right_open = 
                MeasureOpenness(
                    Quaternion.Euler(
                        0f, 
                        start_direction + 90f, 
                        0f) * Vector3.forward
                );

            // to the left of the guard's starting direction
            // get the "score" it got for how open it is
            float left_open = 
                MeasureOpenness(
                    Quaternion.Euler(
                        0f, 
                        start_direction - 90f, 
                        0f) * Vector3.forward
                );

            // what sign to apply to the direction we will want
            // positive means to the right, negative means to the left
            float target_sign;

            // the the right is more open or equal in openness to the left
            // rotate to the right
            if (right_open >= left_open)
                target_sign = 1f;

            // otherwise the left direction is more open, so rotate
            // to the left
            else
                target_sign = -1f;

            // start by rotating to the opposite of where we are facing
            // in the target direction we calculated 
            yield return RotateToAngle(opposite_direction, target_sign);

            // pause in direction we are facing to make guard hold and angle
            yield return new WaitForSeconds(pause_duration);

            // now scan again for which rotation would face more space
            // but now with the guard facing the opposite_direction
            float right_open_opposite =
                MeasureOpenness(
                    Quaternion.Euler(
                        0f, 
                        opposite_direction + 90f, 
                        0f) * Vector3.forward
                );

            float left_open_opposite = 
                MeasureOpenness(
                    Quaternion.Euler(
                        0f, 
                        opposite_direction - 90f, 
                        0f) * Vector3.forward
                );

            float target_sign_opposite;

            if (right_open_opposite >= left_open_opposite)
                target_sign_opposite = 1f;

            else
                target_sign_opposite = -1f;

            yield return RotateToAngle(start_direction, target_sign_opposite);

            yield return new WaitForSeconds(pause_duration);
        }
    }


    // patrol guard walks between point_a and point_b, rotating toward
    // the next target using openness-based direction when turning,
    // just like the static search guard does when scanning
    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // move toward the current target point at patrol speed
            guard.speed = patrol_speed;
            SetGuardDestination(target_point.position);

            // keep facing movement direction until we arrive
            while (HasReachedDestination() == false)
            {
                FaceMovementDirection();
                yield return null;
            }

            // reached the target, reset path and switch to the other point
            guard.ResetPath();

            if (target_point == point_a)
                target_point = point_b;

            else
                target_point = point_a;

            // measure openness left and right to decide
            // which direction to rotate toward the next target
            float right_open = MeasureOpenness(
                Quaternion.Euler(0f, 90f, 0f) * transform.forward
            );

            float left_open = MeasureOpenness(
                Quaternion.Euler(0f, -90f, 0f) * transform.forward
            );

            float sign;
            if (right_open >= left_open)
                sign = 1f;

            else
                sign = -1f;

            // get the angle to the next target point
            float target_direction = RotationFromDirection(
                (target_point.position - transform.position).normalized
            ).eulerAngles.y;    

            // rotate toward the next target using openness direction
            yield return RotateToAngle(target_direction, sign);

            // pause before walking again
            yield return new WaitForSeconds(pause_duration);
        }
    }


    // rotate the guard to the target direction, rotating based on the sign
    private IEnumerator RotateToAngle(float target_direction, float sign)
    {
        // get the distance in angle to rotate to get the guard to the target
        float delta_angle = 
            Mathf.DeltaAngle(transform.eulerAngles.y, target_direction);

        // how much of the angle we need to travel
        // based on the signs direction
        float remaining_angle = sign * delta_angle;

        // if remaining_angle is negative, meaning the most open rotation
        // is to the left, we add 360 degrees to it to get the positive
        // equivalent to the rotation, wraps around to 360
        if (remaining_angle < 0f)
            remaining_angle += 360f;

        // loop until the remaining_angle is covered
        while (remaining_angle > 1f)
        {
            // get the step increment to rotate per
            // loop on a frame-by-frame basis
            // either the amount we can turn this frame, or if the
            // remaining angle is less than the amount we can turn this frame
            float step = 
                Mathf.Min(turn_speed * Time.deltaTime, remaining_angle);

            // rotate the guard on a frame-by-frame basis
            // by the step multiplied by the sign
            transform.rotation = Quaternion.Euler(
                0f, 
                transform.eulerAngles.y + sign * step, 
                0f
            );

            // decrement the remaining angle the guard has to rotate
            // by the step applied this frame
            remaining_angle -= step;

            yield return null;
        }

        // set the guard to be its final target direction to 
        // ensure the angle the guard is facing is correct
        transform.rotation = 
            Quaternion.Euler(0f, target_direction, 0f);
    }
}
