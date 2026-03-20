using System.Collections.Generic;
using UnityEngine;


public class PlayerVehicleInteractor : MonoBehaviour
{
    [SerializeField] private LayerMask vehicleLayer;

    private readonly List<VehicleInteraction> _nearby = new();

    public VehicleInteraction ClosestVehicle { get; private set; }
    public VehicleInteraction CurrentVehicle { get; private set; }
    public bool InVehicle => CurrentVehicle != null;

    private void Update()
    {
        if (!InVehicle)
            UpdateClosest();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (InVehicle)
            {
                CurrentVehicle.Exit();
                CurrentVehicle = null;
            }
            else if (ClosestVehicle != null && !ClosestVehicle.IsOccupied)
            {
                CurrentVehicle = ClosestVehicle;
                CurrentVehicle.Enter(GetComponent<PlayerController>());
            }
        }
    }

    private void UpdateClosest()
    {
        _nearby.RemoveAll(v => v == null);

        VehicleInteraction best = null;
        float bestDist = float.MaxValue;

        foreach (var vehicle in _nearby)
        {
            float dist = Vector3.Distance(transform.position, vehicle.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = vehicle;
            }
        }

        ClosestVehicle = best;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & vehicleLayer) == 0) return;

        var vehicle = other.GetComponentInParent<VehicleInteraction>();
        if (vehicle != null && !_nearby.Contains(vehicle))
            _nearby.Add(vehicle);
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & vehicleLayer) == 0) return;

        var vehicle = other.GetComponentInParent<VehicleInteraction>();
        if (vehicle != null)
            _nearby.Remove(vehicle);
    }
}