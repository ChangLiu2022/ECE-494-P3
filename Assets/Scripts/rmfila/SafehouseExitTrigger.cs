using UnityEngine;
using UnityEngine.SceneManagement;

public class SafehouseExitTrigger : MonoBehaviour
{
    [SerializeField] private string tutorial_scene = "Tutorial";
    [SerializeField] private string newmap_scene = "NewMap";
    [SerializeField] private string not_ready_message = "Collect the gun and map before leaving.";
    [SerializeField]private string check_rifle_message = "Try shooting the training dummies to max your weapon.";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Body"))
            return;

        if (!SafehouseState.gun_collected || !SafehouseState.paper_collected)
        {
            InformationBoxController.instance.Show(not_ready_message);
            return;
        }

        if(!SafehouseState.reached_rifle)
        {
            InformationBoxController.instance.Show(check_rifle_message);
            return;
        }

        if (!SafehouseState.completed_tutorial)
            FadeManager.Instance.StartTransition(tutorial_scene, null, 2f);
        else
            FadeManager.Instance.StartTransition(newmap_scene, null, 2f);
    }
}
