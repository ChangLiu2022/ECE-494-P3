using System.Collections;
using UnityEngine;


public class GuardVisionCone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private LayerMask player_mask;
    [SerializeField] private LayerMask wall_mask;

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
        WaitForSeconds delay = new WaitForSeconds(0.15f);

        while (true)
        {
            yield return delay;
            DetectPlayer();
        }
    }


    private void DetectPlayer()
    {
        float detect_radius = vision_cone.GetDetectRadius();
        float view_angle = vision_cone.GetViewAngle();

        Collider[] range_check =
            Physics.OverlapSphere(
                transform.position,
                detect_radius,
                player_mask
            );

        if (range_check.Length != 0)
        {
            Transform target = range_check[0].transform;
            Vector3 direction =
                (target.position - transform.position).normalized;

            float angle_to_player =
                Vector3.Angle(transform.forward, direction);
            float dist_to_player =
                Vector3.Distance(transform.position, target.position);
            float player_radius = 0.5f;
            float edge_offset =
                Mathf.Atan2(player_radius, dist_to_player) * Mathf.Rad2Deg;

            if (angle_to_player - edge_offset < view_angle / 2)
            {
                float distance =
                    Vector3.Distance(transform.position, target.position);

                if (Physics.Raycast(
                    transform.position,
                    direction,
                    distance,
                    wall_mask
                ) == false)
                {
                    // pass the distance so the controller can scale
                    // chase bar fill rate by proximity
                    guard.SpottedPlayer(true, dist_to_player);
                    return;
                }
            }
        }

        guard.SpottedPlayer(false, 0f);
    }
}