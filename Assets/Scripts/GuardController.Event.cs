using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static GameEvents;

public partial class GuardController
{
    private void OnAlertEvent(AlertEvent e)
    {
        player_last_position = player.position;
        Alert();
    }

    private void OnNoiseWaveEvent(NoiseWaveEvent e)
    {
        // is the distance between the guard and the origin of the shot
        // greater than the radius? if so, guard cannot hear, return
        if (Vector3.Distance(transform.position, e.origin) > e.radius) 
            return;

        // used to stor the calculated path from the
        // origin of the noise to the guard's position
        NavMeshPath path = new NavMeshPath();

        NavMesh.CalculatePath(
            e.origin, 
            transform.position, 
            NavMesh.AllAreas, 
            path
        );

        // not a valid path? return if so
        if (path.status == NavMeshPathStatus.PathInvalid || 
            path.status == NavMeshPathStatus.PathPartial)
        {
            return;
        }

        float path_length = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 from = path.corners[i - 1];
            Vector3 to = path.corners[i];

            // loop through the corners of the path and sum up the distances
            // between two adjacent corners to get the total path length
            float segment_length = Vector3.Distance(from, to);
            path_length += segment_length;

            // if the path length is greater than the radius the noise was
            // assigned then we cannot hear the noise
            if (path_length > e.radius) 
                return;

            // then, if we are still in range, we check if there are any
            // doors in between the two corners, 'to' and 'from'. If there
            // are, guard cannot hear the noise. when the door is opened,
            // it automatically moves the door_mask out of the way so the
            // raycast will not hit
            Vector3 direction = (to - from).normalized;
            if (Physics.Raycast(
                from, 
                direction, 
                segment_length, 
                door_mask, 
                QueryTriggerInteraction.Ignore) == true)
            {
                return;
            }
        }

        StartCoroutine(ReactToNoise(
            e.origin, 
            e.is_gunshot, 
            path_length / noise_wave_expand_speed)
        );
    }

    private IEnumerator ReactToNoise(
        Vector3 origin, bool is_gunshot, float delay)
    {
        // wait for the delay of the noise wave's travel speed
        // this is the length of the path divided by the expand speed
        // of the noise. also added a small buffer to not react too quick
        yield return new WaitForSeconds(delay + 0.1f);

        // if its a gunshot, only a few guards will be going to the player
        // so its fine to give them the direct position
        if (is_gunshot == true)
            player_last_position = origin;

        // if not, its an alert, need to give all guards a random value near
        // the origin to not have them all bunch up
        else
        {
            Vector2 r = Random.insideUnitCircle * 0.75f;
            player_last_position = origin + new Vector3(r.x, 0f, r.y);
        }

        Alert();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Bullet") == true) 
        { 
            TakeDamage(collision); 
            return; 
        }

        if (is_staggered == true) 
            return;

        if (collision.CompareTag("Body") == false) 
            return;

        // already alerted and the player touches the guard again
        // end the game right away
        if (is_alerted == true) 
        { 
            EndGame(); 
            return; 
        }

        Alert();

        if (is_catching == false) 
            StartCoroutine(GracePeriodRoutine());
    }

    private void OnTriggerStay(Collider collision)
    {
        // if staggered, do nothing
        if (is_staggered == true || is_alerted == false) 
            return;

        if (collision.CompareTag("Body") == true
            && is_catching == false)
        {
            EndGame();
        }
    }

    // grace period where if the player touches the guard,
    // its not an instant game over
    private IEnumerator GracePeriodRoutine()
    {
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        is_catching = false;
    }
}
