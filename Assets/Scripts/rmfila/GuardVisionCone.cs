using System.Collections;
using UnityEngine;

public class GuardVisionCone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask player_mask;
    [SerializeField] private LayerMask wall_mask;
    [SerializeField] private LayerMask door_mask;

    private VisionConeMesh vision_cone;
    private GuardController guard;


    private void Start()
    {
        guard = GetComponentInParent<GuardController>();
        vision_cone = GetComponent<VisionConeMesh>();

        StartCoroutine(DetectionRoutine());
    }


    private IEnumerator DetectionRoutine()
    {
        // delay how frequently this runs -- performance
        WaitForSeconds delay = new WaitForSeconds(0.05f);

        while (true)
        {
            yield return delay;
            DetectPlayer();
        }
    }


    // uses overlap sphere to find player, then  determines if the player
    // is within the guard's vision cone by checking the angle to the player
    // and checking if there are any walls or doors in the way, if the player
    // can be detected, the guard's spotted player function is called true
    private void DetectPlayer()
    {
        float detect_radius = vision_cone.GetDetectRadius();
        float view_angle = vision_cone.GetViewAngle();

        Collider[] range_check = Physics.OverlapSphere(
            transform.position, 
            detect_radius, 
            player_mask
        );

        if (range_check.Length != 0)
        {
            // only 1 player, so will always be at index 0
            Transform target = range_check[0].transform;
            Vector3 direction = (target.position - transform.position).normalized;

            float angle_to_player = 
                Vector3.Angle(transform.forward, direction);

            float dist_to_player = 
                Vector3.Distance(transform.position, target.position);

            float player_radius = 0.6f;

            // the edge offset is the angle between the center of the player
            // and the edge of the player, this is used to prevent the guard
            // from only being able to see the center of the player, and
            // allows them to see the player if they are partially in the
            // vision cone
            float edge_offset = 
                Mathf.Atan2(player_radius, dist_to_player) * Mathf.Rad2Deg;

            // if the nearest edge of the player is within the view angle
            // check if there are any walls or doors in the way, if not
            // the player is spotted.
            if (angle_to_player - edge_offset < view_angle / 2)
            {
                if (Physics.Raycast(
                    transform.position, 
                    direction, 
                    dist_to_player,
                    wall_mask | door_mask) == false)
                {
                    guard.SpottedPlayer(true);
                    return;
                }
            }
        }

        // player not in sphere distance, not possible to see
        guard.SpottedPlayer(false);
    }
}