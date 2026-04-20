using UnityEngine;

public class SafehouseLoader : MonoBehaviour
{
    [Header("Pickups")]
    [SerializeField] private GameObject pistol_full;
    [SerializeField] private GameObject paper;

    [Header("Workbench Flash")]
    [SerializeField] private Workbench workbench;

    [Header("Exit")]
    [SerializeField] private GameObject exit_prefab;

    private void Start()
    {
        if (SafehouseState.gun_collected && pistol_full != null)
            pistol_full.SetActive(false);

        if (SafehouseState.paper_collected && paper != null)
            paper.SetActive(false);

        if (workbench != null)
            workbench.enabled = SafehouseState.completed_tutorial
                                   && !SafehouseState.workbench_interacted;

        if (exit_prefab != null)
            exit_prefab.SetActive(SafehouseState.completed_final_map);
    }
}
