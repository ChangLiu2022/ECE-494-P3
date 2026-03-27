using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PoliceVehicleNavMesh : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Transform _target;
    private bool _chasing;
    [SerializeField] private Vector3 TransformOffset;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false;
        StartCoroutine(EnableAgentNextFrame());
    }


    private IEnumerator EnableAgentNextFrame()
    {
        yield return null;
        _agent.enabled = true;
        _agent.isStopped = true;
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
        _agent.isStopped = false;
    }

    private void OnVehicleExit(GameEvents.VehicleExitEvent evt)
    {
        _chasing = false;
        _target = null;
        _agent.isStopped = true;
    }

    private void Update()
    {
        if (_chasing && _target != null)
            _agent.SetDestination(_target.position + TransformOffset);
    }
}
