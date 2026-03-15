using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

// used over calling GameEvents.AlertEvent
using static GameEvents;


public class GuardController : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Toggle between static and patrolling guard.")]
    [SerializeField] private bool is_patrol = false;
    [Tooltip("Drag two empty game objects as patrol points.")]
    [SerializeField] private Transform point_a;
    [SerializeField] private Transform point_b;
    [SerializeField] private float patrol_speed = 3f;
    [Tooltip("How long the guard pauses at each endpoint " +
        "before turning around.")]
    [SerializeField] private float pause_duration = 1.5f;

    [Header("Chase Settings")]
    [Tooltip("Gonna be honest, after adding the navmesh agent, " +
        "idk how this effects the speed fully.")]
    [SerializeField] private float chase_speed = 2f;

    private bool is_chasing = false;

    // patrolling state
    // the target point guard is walking to
    private Transform target_point;
    // guard needs to pause once it gets to the destination
    private bool is_paused = false;
    // how long the guard has waited
    private float pause_time = 0f;

    // -- OLD
    // private Rigidbody rb;
    // -- NEW
    private NavMeshAgent guard;
    // PLAYER NEEDS TO HAVE TAG 'Player'
    private Transform player;


    public void SpottedPlayer()
    {
        if (is_chasing)
            return;

        // spotted player set true and set agent's speed
        is_chasing = true;
        is_paused = false;
        guard.speed = chase_speed;

        Debug.Log(gameObject.name + " spotted the player, chasing!");
    }


    private void OnEnable()
    {
        // subscribe to the collectible being grabbed
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
    }


    private void Awake()
    {
        /* -- OLD
        rb = GetComponent<Rigidbody>();
        // less demanding than dynamic and we only want
        // the guards to move via explicit repositioning
        rb.isKinematic = true;
        */
        // -- NEW
        guard = GetComponentInChildren<NavMeshAgent>();

        // keep guard flat for top-down
        guard.updateRotation = false;
        guard.updateUpAxis = false;
    }


    private void Start()
    {
        // wait till game starts, then cache a
        // reference to the player for later use
        var player_object = GameObject.FindWithTag("Player");

        if (player_object != null)
            player = player_object.transform;
        else
            Debug.Log("No GameObject with the tag 'Player' was found.");

        // if patrol is set, head towards point_a
        if (is_patrol == true && point_a != null && point_b != null)
        {
            target_point = point_a;
            // move at patrol speed
            guard.speed = patrol_speed;
            guard.SetDestination(target_point.position);
        }
    }


    // detection to see if player was spotted and to chase them
    private void FixedUpdate()
    {
        if (is_chasing == true && player != null)
            ChasePlayer();

        else if (is_patrol && is_chasing == false)
            Patrol();
    }


    private void ChasePlayer()
    {
        /* -- OLD
        Vector3 dir = 
            (player.position - transform.position).normalized;

        rb.MovePosition(
            transform.position + dir * move_speed * Time.fixedDeltaTime);

        // rotate the guard to face the player around the y-axis
        // achieved via Atan2, takes the distances we found to the player
        // and converts those X and Y vals to be the rotation pointing
        // in that dir
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        // preserve the 90 degree x axis. or not,
        // IDK what we are going for exactly
        rb.MoveRotation(Quaternion.Euler(90f, angle, 0f));
        */
        // -- NEW
        // navmesh will handle pathfinding around walls
        guard.SetDestination(player.position);

        // manually rotate to face the movement direction
        // only if we are moving substantially enough
        if (guard.velocity.sqrMagnitude > 0.01f)
        {
            // the current direction the guard is going
            Vector3 direction = guard.velocity.normalized;
            // rotate the guard to face the player around the y-axis
            // achieved via Atan2, takes distances we found to the player
            // and converts those X and Y vals to be the rotation pointing
            // in that direction
            float angle = 
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // rotate the guard, this will point the vision cone too
            // since we are casting out as forward
            // changed to match Patrol as lerp is better
            Quaternion target_rotation = Quaternion.Euler(0f, angle, 0f);
            // lerp instead of instantly setting the direction 
            // this smoothly pans the guards looking when patrolling.
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                target_rotation,
                10f * Time.fixedDeltaTime
            );
        }
    }


    private void Patrol()
    {
        if (target_point == null)
            return;

        // if paused at endpoint, countdown to move again
        if (is_paused == true)
        {
            // decrement pause time
            pause_time -= Time.fixedDeltaTime;

            // pause time expired, unpause and move
            if (pause_time <= 0f)
            {
                is_paused = false;
                guard.SetDestination(target_point.position);
            }

            // otherwise do nothing/remain still/paused
            return;
        }
        
        // same thing as before to get the guard facing the direction
        // they are moving, but now when patrolling
        if (guard.velocity.sqrMagnitude > 0.01f)
        {
            // the current direction the guard is going
            Vector3 direction = guard.velocity.normalized;
            // rotate the guard to face the player around the y-axis
            // achieved via Atan2, takes distances we found to the player
            // and converts those X and Y vals to be the rotation pointing
            // in that direction
            float angle =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // rotate the guard, this will point the vision cone too
            // since we are casting out as forward
            Quaternion target_rotation = Quaternion.Euler(0f, angle, 0f);
            // lerp instead of instantly setting the direction 
            // this smoothly pans the guards looking when patrolling.
            transform.rotation = Quaternion.Lerp(
                transform.rotation, 
                target_rotation, 
                10f * Time.fixedDeltaTime
            );
        }

        // check if the guard has reached the target point by
        // checking to see agent  is  still computing  a path
        if (guard.pathPending == false && guard.remainingDistance <=
            guard.stoppingDistance)
        {
            // pause at the endpoint as the guard has reached the target
            is_paused = true;
            // set the timer countdown
            pause_time = pause_duration;
            guard.ResetPath();

            // update the target_point to be the opposite point
            if (target_point == point_a)
                target_point = point_b;
            else
                target_point = point_a;
        }
    }


    private void OnAlertEvent(AlertEvent e)
    {
        // all guards are alerted when collecible is collected
        is_chasing = true;
        is_paused = false;
        guard.speed = chase_speed;

        Debug.Log(gameObject.name + " received AlertEvent, chasing!");
    }


    private void OnTriggerEnter(Collider collision)
    {
        // DETECTION IS TRIGGERED BY BULLET PROJECTILE HAVING TAG BULLET
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        // only catch player if already chasing, this makes it so if
        // a player rushes through and collides with a guard, it won't
        // be an immediate game over, only after the chase starts can
        // the guards catch the  player
        if (is_chasing == false)
            return;

        if (collision.CompareTag("Player"))
        {
            Debug.Log("The player has been caught, Game Over!");
            EventBus.Publish(new GameOverEvent());
        }
    }
}


// credits
// https://docs.unity3d.com/2022.2/Documentation/Manual/Rigidbody2D-Kinematic.html
//
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html
