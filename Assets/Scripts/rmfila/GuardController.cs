using System.Collections;
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

    // set to public to expose to vision cone mesh for color swap
    public bool is_chasing = false;

    // patrolling state
    // the target point guard is walking to
    private Transform target_point;
    // guard needs to pause once it gets to the destination
    private bool is_paused = false;
    // how long the guard has waited
    private float pause_time = 0f;

    // this is to give grace period to player if they bump into guard
    // idk, it felt wierd that when the player ran into the guard,
    // it immediately game overed with no feedback. I feel like seeing
    // the guard flip to chase mode and "attack" the player felt better
    private bool is_catching = false;

    private NavMeshAgent guard;
    private Transform player;


    public void SpottedPlayer()
    {
        // already chasing
        if (is_chasing)
            return;

        // spotted player start chasing
        is_chasing = true;
        is_paused = false;
        guard.speed = chase_speed;

        Debug.Log(gameObject.name + " spotted the player, chasing!");
    }


    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
    }


    private void Awake()
    {
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
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Player") == false)
            return;

        // already chasing = instant game over, no grace
        if (is_chasing)
        {
            Debug.Log("The player has been caught, Game Over!");
            EventBus.Publish(new GameOverEvent());
            return;
        }

        // first contact = start chasing and give 1 second to flee
        SpottedPlayer();

        if (is_catching == false)
            StartCoroutine(GracePeriod());
    }


    // only can be called 1 time and thats on the initial contact with guard
    // after this, if the player doesn't move from the guard collision field
    // by the grace timer's end, it will be called again and have is_catching
    // = to false, so it will game over. Otherwise, if the player leaves and
    // enters again, that will call OnTriggerEnter, and this cannot be called
    // again.
    private void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Destroy(gameObject);
            return;
        }

        // still touching after grace period expired = game over
        if (collision.CompareTag("Player") && 
            is_chasing && is_catching == false)
        {
            Debug.Log("The player has been caught, Game Over!");
            EventBus.Publish(new GameOverEvent());
        }
    }


    // gives player that touched guard a moment of time
    private IEnumerator GracePeriod()
    {
        // now can no longer get grace period
        is_catching = true;
        yield return new WaitForSeconds(0.5f);
        // set to false that way on trigger enter can call a game over
        // either way, this cannot be called again as explained above
        is_catching = false;
    }
}


// credits
// https://docs.unity3d.com/2022.2/Documentation/Manual/Rigidbody2D-Kinematic.html
//
// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.Atan2.html
