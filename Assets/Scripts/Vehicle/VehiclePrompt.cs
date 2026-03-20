using UnityEngine;

public class VehiclePrompt : MonoBehaviour
{
    [SerializeField] private VehicleInteraction interaction;
    [SerializeField] private GameObject prompt;

    private PlayerVehicleInteractor _interactor;

    private void Start()
    {
        _interactor = FindObjectOfType<PlayerVehicleInteractor>();
    }

    private void Update()
    {
        if (prompt == null || _interactor == null) return;

        prompt.SetActive(
            !interaction.IsOccupied
            && !_interactor.InVehicle
            && _interactor.ClosestVehicle == interaction
        );
    }
}