using UnityEngine;

public class PoliceVehicleMovement : MonoBehaviour
{
    /*
    [Header("Chase")]
    [SerializeField] private float engineForce = 25f;
    [SerializeField] private float maxSpeed = 18f;
    [SerializeField] private float steerSpeed = 200f;

    [Header("Physics")]
    [SerializeField] private float traction = 8f;
    [SerializeField] private float rollingDrag = 0.3f;

    [Header("Collision")]
    [SerializeField] private float impactSpeedLoss = 0.5f;
    [SerializeField] private LayerMask vehicleLayer;

    private Vector3 _velocity;
    private Transform _target;
    private bool _chasing;
    private Collider[] _overlapResults = new Collider[10];
    private BoxCollider _boxCollider;

    public Vector3 Velocity => _velocity;

    private void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameEvents.VehicleEnterEvent>(OnVehicleEnter);
        EventBus.Subscribe<GameEvents.VehicleExitEvent>(OnVehicleExit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameEvents.VehicleEnterEvent>(OnVehicleEnter);
        EventBus.Unsubscribe<GameEvents.VehicleExitEvent>(OnVehicleExit);
    }

    private void OnVehicleEnter(GameEvents.VehicleEnterEvent evt)
    {
        _target = evt.vehicleTransform;
        _chasing = true;
    }

    private void OnVehicleExit(GameEvents.VehicleExitEvent evt)
    {
        _chasing = false;
        _target = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_chasing && _target != null)
        {
            Vector3 forward = transform.right;

            // Steer toward target
            Vector3 toTarget = (_target.position - transform.position).normalized;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, steerSpeed * dt);
            }

            // Accelerate
            forward = transform.right;
            float forwardSpeed = Vector3.Dot(_velocity, forward);

            if (forwardSpeed < maxSpeed)
                _velocity += forward * engineForce * dt;
        }

        // Physics always runs (coasting when not chasing)
        float speed = _velocity.magnitude;
        Vector3 fwd = transform.right;
        float fwdSpeed = Vector3.Dot(_velocity, fwd);
        Vector3 forwardVel = fwd * fwdSpeed;
        Vector3 sidewaysVel = _velocity - forwardVel;
        sidewaysVel = Vector3.MoveTowards(sidewaysVel, Vector3.zero, traction * speed * dt);
        _velocity = forwardVel + sidewaysVel;

        if (speed > 0.01f)
            _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, rollingDrag * dt);

        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);

        transform.position += _velocity * dt;
    }
    */
}