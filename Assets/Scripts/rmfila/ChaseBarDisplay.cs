using UnityEngine;
using UnityEngine.UI;


// this script controls changing the white bar to accurately
// display how long the guard will search for the player
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
        if (guard_controller.current_tier >= GuardTier.Tier3 == true)
            // display chase bar when chasing
            canvas_group.alpha = 1f;
        else
            // disable it otherwise
            canvas_group.alpha = 0f;
    }
}