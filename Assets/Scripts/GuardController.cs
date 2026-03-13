using UnityEngine;
using UnityEngine.AI;

// used over calling GameEvents.AlertEvent
using static GameEvents;


public class GuardController : MonoBehaviour
{
    [Header("Chase Settings")]
    [Tooltip("For GoldSpike, value must be higher than the player's")]
    [SerializeField] private float move_speed = 2f;

    private bool is_chasing = false;

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
        guard.speed = move_speed;

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
        guard = GetComponent<NavMeshAgent>();
        guard.speed = move_speed;

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
    }


    // detection to see if player was spotted and to chase them
    private void FixedUpdate()
    {
        if (is_chasing && player != null)
            ChasePlayer();
    }


    // THIS APPROACH IS TEMPORARY AS I NEED TO BAKE AN AI NAV MESH INTO
    // THE MAP IN ORDER FOR THE GUARDS TO NAVIGATE THE MAP CORRECTLY
    private void ChasePlayer()
    {
        /* -- OLD
        Vector3 direction = 
            (player.position - transform.position).normalized;

        rb.MovePosition(
            transform.position + direction * move_speed * Time.fixedDeltaTime);

        // rotate the guard to face the player around the y-axis
        // achieved via Atan2, takes the distances we found to the player
        // and converts those X and Y vals to be the rotation pointing
        // in that direction
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
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
            Vector3 direction = guard.velocity.normalized;
            // rotate the guard to face the player around the y-axis
            // achieved via Atan2, takes the distances we found to the player
            // and converts those X and Y vals to be the rotation pointing
            // in that direction
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // preserve the 90 degree x axis. or not,
            // IDK what we are going for exactly
            transform.rotation = Quaternion.Euler(90f, angle, 0f);
        }
    }


    private void OnAlertEvent(AlertEvent e)
    {
        // all guards are alerted when collecible is collected
        is_chasing = true;

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
