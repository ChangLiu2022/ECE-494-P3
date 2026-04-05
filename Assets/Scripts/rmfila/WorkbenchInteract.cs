using UnityEngine;

public class WorkbenchInteract : MonoBehaviour
{
    [SerializeField] private float interact_range = 2f;
    [SerializeField] private KeyCode interact_key = KeyCode.E;
    [SerializeField] private Transform player;

    [Header("Buy Menu")]
    [SerializeField] private GameObject buy_menu_panel;

    private Workbench workbench_flash;


    private void Start()
    {
        workbench_flash = GetComponent<Workbench>(); // add this
        if (SafehouseState.workbench_interacted)
        {
            if (workbench_flash != null) workbench_flash.enabled = false;
            enabled = false;
            return;
        }
    }


    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interact_range && Input.GetKeyDown(interact_key))
        {
            SafehouseState.workbench_interacted = true;

            if (workbench_flash != null) workbench_flash.enabled = false;

            OpenBuyMenu();

            // disable this script so E can never trigger it again
            enabled = false;
        }
    }


    private void OpenBuyMenu()
    {
        if (buy_menu_panel != null)
            buy_menu_panel.SetActive(true);

        // TODO: populate and display gun upgrade buy menu
        // TODO: hook up upgrade buttons to player inventory/weapon stats
        // TODO: close button should call CloseBuyMenu()
    }

    private void CloseBuyMenu()
    {
        if (buy_menu_panel != null)
            buy_menu_panel.SetActive(false);
    }
}
