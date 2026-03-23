using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float engineForce = 30f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float brakeForce = 15f;

    [Header("Steering")]
    [SerializeField] private float steerAngle = 120f;
    [SerializeField] private float minSpeedToSteer = 0.5f;

    [Header("Physics")]
    [Tooltip("How quickly sideways velocity aligns to facing. Low = more drift.")]
    [SerializeField] private float traction = 10f;
    [Tooltip("Traction when braking into a turn. Lower = more drift.")]
    [SerializeField] private float driftTraction = 1.5f;
    [Tooltip("Speed lost per second with no input. Lower = more coast.")]
    [SerializeField] private float rollingDrag = 0.5f;

    private Vector3 _velocity;
    private bool _active;
    private Rigidbody _rb;
    private float _throttle;
    private float _steer;

    public Vector3 Velocity => _velocity;
    public bool IsActive => _active;

    public void SetActive(bool active)
    {
        _active = active;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = true;
    }

    private void Update()
    {
        _throttle = _active ? Input.GetAxis("Vertical") : 0f;
        _steer = _active ? Input.GetAxis("Horizontal") : 0f;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 forward = transform.right;

        float forwardSpeed = Vector3.Dot(_velocity, forward);
        Vector3 forwardVel = forward * forwardSpeed;
        Vector3 sidewaysVel = _velocity - forwardVel;

        // Engine / brake
        if (_throttle > 0f)
        {
            if (forwardSpeed < -0.5f)
                _velocity += forward * brakeForce * _throttle * dt;
            else if (forwardSpeed >= -0.5f && forwardSpeed <= 0.01f)
                _velocity += forward * engineForce * 0.2f * _throttle * dt;
            else
                _velocity += forward * engineForce * _throttle * dt;
        }
        else if (_throttle < 0f)
        {
            if (forwardSpeed > 0.5f)
                _velocity += forward * brakeForce * _throttle * dt;
            else if (forwardSpeed <= 0.5f && forwardSpeed >= -0.01f)
                _velocity += forward * engineForce * 0.2f * _throttle * dt;
            else
                _velocity += forward * engineForce * 0.4f * _throttle * dt;
        }

        // Steering
        float speed = _velocity.magnitude;
        if (speed > minSpeedToSteer)
        {
            Quaternion rot = Quaternion.Euler(0f, _steer * steerAngle * dt, 0f);
            _rb.MoveRotation(_rb.rotation * rot);
        }

        // Traction
        forward = transform.right;
        forwardSpeed = Vector3.Dot(_velocity, forward);
        forwardVel = forward * forwardSpeed;
        sidewaysVel = _velocity - forwardVel;

        bool drifting = _throttle < 0f && Mathf.Abs(_steer) > 0.1f && forwardSpeed > 1f;
        float currentTraction = drifting ? driftTraction : traction;
        sidewaysVel = Vector3.MoveTowards(sidewaysVel, Vector3.zero, currentTraction * speed * dt);
        _velocity = forwardVel + sidewaysVel;

        // Rolling drag
        if (speed > 0.01f)
            _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, rollingDrag * dt);

        // Max speed
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);

        // Apply
        _rb.MovePosition(_rb.position + _velocity * dt);
    }
}