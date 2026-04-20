using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ExitDoorScript : MonoBehaviour
{
    [SerializeField] public float interactRange = 1.5f;
    [SerializeField] private string target_scene = "Safehouse";
    [SerializeField] private bool set_tutorial_complete = false;
    [SerializeField] private bool set_newmap_complete = false;
    [SerializeField] private bool set_map2_complete = false;
    [SerializeField] private bool set_final_map_complete = false;
    [SerializeField] private string no_gold_message = "You need to collect the gold before leaving!";
    [SerializeField] private Canvas canvas;
    private bool exiting = false;

    private void OnTriggerEnter(Collider coll)
    {
        if (exiting) return;

        if (!coll.CompareTag("Player")) return;

        if (!PlayerWallet.level_reward_claimed)
        {
            InformationBoxController.instance.Show(no_gold_message);
            return;
        }

        exiting = true;

        if (set_tutorial_complete)
            SafehouseState.completed_tutorial = true;

        if (set_newmap_complete)
            SafehouseState.completed_newmap = true;

        if (set_map2_complete)
            SafehouseState.completed_map_2 = true;

        if (set_final_map_complete)
            SafehouseState.completed_final_map = true;

        PlayerWallet.AdvanceLevel();
        InformationBoxController.instance.gameObject.SetActive(false);
        FadeManager.Instance.StartTransition(target_scene, null, 1.3f);
    }
}