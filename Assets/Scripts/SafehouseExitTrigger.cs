using UnityEngine;

public class SafehouseExitTrigger : MonoBehaviour
{
    [SerializeField] private string tutorial_scene = "Tutorial";
    [SerializeField] private string newmap_scene = "NewMap";
    [SerializeField] private string not_ready_message = "Collect the gun and map before leaving.";
    [SerializeField] private string map_not_picked_up_message = "Collect the new map before leaving.";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Body"))
            return;

        if (!SafehouseState.gun_collected || !SafehouseState.paper_collected_once)
        {
            InformationBoxController.instance.Show(not_ready_message);
            return;
        }

        if(!SafehouseState.paper_collected)
        {
            InformationBoxController.instance.Show(map_not_picked_up_message);
            return;
        }

        if (!SafehouseState.completed_tutorial)
            FadeManager.Instance.StartTransition(tutorial_scene, null, 1.95f);
        else
            FadeManager.Instance.StartTransition(newmap_scene, null, 2f);
    }
}
