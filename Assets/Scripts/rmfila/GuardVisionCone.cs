using System.Collections;
using UnityEngine;


public class GuardVisionCone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask player_mask;
    [SerializeField] private LayerMask wall_mask;

    // ref to the mesh so detection always matches the visual
    // also less stuff needed to be duplicated, keeping it DRY as they say
    private VisionConeMesh vision_cone;
    private GuardController guard;


    private void Start()
    {
        guard = GetComponentInParent<GuardController>();
        vision_cone = GetComponent<VisionConeMesh>();

        // start routine on right away
        StartCoroutine(DetectionRoutine());
    }


    private IEnumerator DetectionRoutine()
    {
        WaitForSeconds delay = new WaitForSeconds(0.25f);

        while (true)
        {
            // wait the delay to not run this more than needed
            yield return delay;

            // call function to detect player after delay time
            DetectPlayer();
        }
    }


    private void DetectPlayer()
    {
        float detect_radius = vision_cone.GetDetectRadius();
        float view_angle = vision_cone.GetViewAngle();

        // cast a sphere around the guard to capture player in
        // should only grab player if player_mask is player's layer
        Collider[] range_check = 
            Physics.OverlapSphere(
                transform.position, 
                detect_radius, 
                player_mask
            );

        if (range_check.Length != 0)
        {
            // only player should exist on player_mask
            Transform target = range_check[0].transform;
            Vector3 direction = 
                (target.position - transform.position).normalized;

            // checks if the angle formed by the direction the guard
            // is facing and the direction to the player is less than
            // half the assigned viewing angle. This is for half the
            // angle left, and half the angle right
            float angle_to_player = 
                Vector3.Angle(transform.forward, direction);
            float dist_to_player = 
                Vector3.Distance(transform.position, target.position);
            float player_radius = 0.5f;
            float edge_offset = 
                Mathf.Atan2(player_radius, dist_to_player) * Mathf.Rad2Deg;

            // angle_to_player - edge_offset is basically saying the angle
            // between the line from the guard to the player's center (using
            // this alone was causing me bugs where the player was in the
            // guard's vision cone but not being detected) and a line from
            // the guard to the player's radius edge is the offset we need
            // to subtract from out line just to the player's center. This
            // makes it so now the player is detect when the edge of it 
            // enters the guard's vision cone
            if (angle_to_player - edge_offset < view_angle / 2)
            {
                float distance = 
                    Vector3.Distance(transform.position, target.position);

                // start a ray cast from the guard in the direction of the
                // player, the distance we solved for to the player (no
                // overshooting), and stop the raycast if it hits walls
                if (Physics.Raycast(
                    transform.position, 
                    direction, 
                    distance,
                    wall_mask
                ) == false)
                {
                    // player is visible
                    guard.SpottedPlayer(true);
                    return;
                }
            }
        }

        // player is not visible this tick
        guard.SpottedPlayer(false);
    }
}


// credits
// https://youtu.be/j1-OyLo77ss