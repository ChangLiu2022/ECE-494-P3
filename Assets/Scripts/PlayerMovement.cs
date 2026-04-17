using UnityEngine;
using static GameEvents;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 50f;

    private AudioSource footstepAudio;

    private Rigidbody rb;
    private Vector3 velocity;

    public Vector3 Velocity => velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        footstepAudio = GetComponent<AudioSource>();
        if (footstepAudio == null) Debug.LogError("No AudioSource found on PlayerMovement!");
    }

    private void Update()
    {
        Vector3 input = GetInput();
        UpdateVelocity(input);

        if(footstepAudio != null) HandleFootsteps(input);
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
    }

    private Vector3 GetInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(h, 0f, v);
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        return dir;
    }

    private void UpdateVelocity(Vector3 input)
    {
        
        Vector3 target = input * moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            target *= sprintMultiplier;
        }
        float rate = (input.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        velocity = Vector3.MoveTowards(velocity, target, rate * Time.deltaTime);
        velocity.y = 0f;
    }

    private void HandleFootsteps(Vector3 input)
    {
        if(GameFreezer.IsFrozen)
        {
            if (footstepAudio.isPlaying)
            {
                footstepAudio.Stop();
            }
            return;
        }

        bool isMoving = input.sqrMagnitude > 0.01f;
        
        footstepAudio.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.7f : 1.2f;

        if (isMoving && !footstepAudio.isPlaying)
        {
            footstepAudio.Play();
        }
        else if (!isMoving && footstepAudio.isPlaying)
        {
            footstepAudio.Stop();
        }
    }

    public void AddImpulse(Vector3 impulse)
    {
        velocity += impulse;
    }

    public void SetVelocity(Vector3 vel)
    {
        velocity = vel;
    }
}