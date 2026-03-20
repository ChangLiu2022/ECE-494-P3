using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChaseBarDisplay : MonoBehaviour
{
    // the bar that shows how much time is left for chasing
    [SerializeField] private Image fill_image;
    // controls vistability
    [SerializeField] private CanvasGroup canvas_group;

    private GuardController guard_controller;


    private void Start()
    {
        guard_controller = GetComponentInParent<GuardController>();
        // disable the view esstentially for it
        canvas_group.alpha = 0f;
    }


    private void LateUpdate()
    {
        // the ratio of the current chase bar time / the max time delay
        fill_image.fillAmount = guard_controller.GetChaseBarRatio();

        // guard tier 3 or 4 = chasing
        bool is_chasing = guard_controller.current_tier >= GuardTier.Tier3;

        if (is_chasing == true)
            // display chase bar when chasing
            canvas_group.alpha = 1f;

        else
            // disable it otherwise
            canvas_group.alpha = 0f;
    }
}