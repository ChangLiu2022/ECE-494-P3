using System.Collections;
using UnityEngine;

public class GuardVisionCone : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detect_radius = 5f;
    [SerializeField] private float view_angle = 90f;
    // player layer
    [SerializeField] LayerMask target_mask;
    // wall layer
    [SerializeField] LayerMask obstruction_mask;

    // PLAYER NEEDS TAG "Player" IN ORDER TO WORK
    private GameObject player;
    private GuardController guard;


    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        guard = GetComponentInParent<GuardController>();
        
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
        // cast a sphere around the guard to capture player in
        // should only grab player if target_mask is player's layer
        Collider[] range_check = 
            Physics.OverlapSphere(
                transform.position, 
                detect_radius, 
                target_mask
            );

        if (range_check.Length != 0)
        {
            // only player should exist on target_mask
            Transform target = range_check[0].transform;
            Vector3 direction = 
                (target.position - transform.position).normalized;

            // checks if the angle formed by the direction the guard
            // is facing and the direction to the player is less than
            // half the assigned viewing angle. This is for half the
            // angle left, and half the angle right
            if (Vector3.Angle(transform.forward, direction) < view_angle / 2)
            {
                float distance = 
                    Vector3.Distance(transform.position, target.position);

                // start a ray cast from the guard in the direction of the
                // player, the distance we solved for to the player (no
                // overshooting), and stop the raycast if it hits anything
                // in the obstruction_mask (walls, etc...)
                if (Physics.Raycast(
                    transform.position, 
                    direction, 
                    distance, 
                    obstruction_mask
                ) == false)
                {
                    // if this is false, meaning we enter the code for the if
                    // it means we did not hit a wall, and can see the player
                    // call GuardController's SpottedPlayer() func
                    guard.SpottedPlayer();
                }
            }
        }
    }
}


// credits
// https://youtu.be/j1-OyLo77ss