using UnityEngine;
using UnityEngine.SceneManagement;

public class SafehouseExitTrigger : MonoBehaviour
{
    [SerializeField] private string tutorial_scene = "Tutorial";
    [SerializeField] private string newmap_scene = "NewMap";
    [SerializeField] private string not_ready_message = "Collect the gun and map before leaving.";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Body"))
            return;

        if (!SafehouseState.gun_collected || !SafehouseState.paper_collected)
        {
            InformationBoxController.instance.Show(not_ready_message);
            return;
        }

        if (!SafehouseState.completed_tutorial)
            SceneManager.LoadScene(tutorial_scene);
        else
            SceneManager.LoadScene(newmap_scene);
    }
}
