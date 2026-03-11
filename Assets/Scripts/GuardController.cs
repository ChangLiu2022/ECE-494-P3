using UnityEngine;
// used over calling GameEvents.AlertEvent
using static GameEvents;


public class GuardController : MonoBehaviour
{
    [Header("Chase Settings")]
    [Tooltip("For GoldSpike, value must be higher than the player's")]
    [SerializeField] private float move_speed = 2f;

    private bool is_chasing = false;

    private Rigidbody2D rb;
    // PLAYER NEEDS TO HAVE TAG 'Player'
    private Transform player;


    public void OnPlayerSpotted()
    {
        if (is_chasing == false)
        {
            is_chasing = true;
            Debug.Log(gameObject.name + " spotted the player, chasing!");
        }
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
        rb = GetComponent<Rigidbody2D>();
        // less demanding than dynamic and we only want
        // the guards to move via explicit repositioning
        rb.bodyType = RigidbodyType2D.Kinematic;
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


    private void FixedUpdate()
    {
        if (is_chasing && player != null)
            ChasePlayer();
    }


    // THIS APPROACH IS TEMPORARY AS I NEED TO BAKE AN AI NAV MESH INTO
    // THE MAP IN ORDER FOR THE GUARDS TO NAVIGATE THE MAP CORRECTLY
    private void ChasePlayer()
    {
        // cast transform's Vector3 to a Vector2
        Vector2 distance = 
            ((Vector2)player.position - rb.position).normalized;
        
        // move the kinematic rb towards the player
        rb.MovePosition(
            rb.position + distance * move_speed * Time.fixedDeltaTime);

        // rotate the guard to face the player
        // achieved via Atan2, takes the distances we found to the player
        // and converts those X and Y vals to be the rotation pointing
        // in that direction
        rb.MoveRotation(Mathf.Atan2(distance.y, distance.x) * Mathf.Rad2Deg);
    }


    private void OnAlertEvent(AlertEvent e)
    {
        is_chasing = true;
        Debug.Log(gameObject.name + " received AlertEvent, chasing!");
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
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
