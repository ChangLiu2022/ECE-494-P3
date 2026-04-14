using UnityEngine;

public class WorkbenchInteract : MonoBehaviour
{
    [SerializeField] private float interact_range = 2f;
    [SerializeField] private KeyCode interact_key = KeyCode.E;
    [SerializeField] private Transform player;

    [Header("Buy Menu")]
    [SerializeField] private BuyMenuController buy_menu;

    private Workbench workbench_flash;

    private void Start()
    {
        workbench_flash = GetComponent<Workbench>();

        if (!SafehouseState.completed_tutorial)
        {
            enabled = false;
            return;
        }

        if (SafehouseState.workbench_interacted && workbench_flash != null)
            workbench_flash.enabled = false;
    }

    private void Update()
    {
        if (!SafehouseState.completed_tutorial) return;
        if (player == null) return;
        if (BuyMenuController.IsOpen) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= interact_range && Input.GetKeyDown(interact_key) && !MapController.is_open)
        {
            SafehouseState.workbench_interacted = true;
            if (workbench_flash != null) workbench_flash.enabled = false;
            if (buy_menu != null) buy_menu.Open();
        }
    }
}
