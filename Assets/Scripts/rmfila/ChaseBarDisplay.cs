using UnityEngine;
using UnityEngine.UI;


public class ChaseBarDisplay : MonoBehaviour
{
    [SerializeField] private Image fill_image;
    [SerializeField] private CanvasGroup canvas_group;

    private GuardController guard_controller;


    private void Start()
    {
        guard_controller = GetComponentInParent<GuardController>();
        canvas_group.alpha = 0f;
    }


    private void LateUpdate()
    {
        float ratio = guard_controller.GetChaseBarRatio();
        fill_image.fillAmount = ratio;

        // show bar whenever it has any fill (spotting or chasing)
        // hide when empty and not actively chasing
        if (ratio > 0f || guard_controller.current_tier >= GuardTier.Tier3)
            canvas_group.alpha = 1f;

        else
            canvas_group.alpha = 0f;
    }
}