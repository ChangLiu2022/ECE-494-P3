using System.Collections;
using UnityEngine;
using static GameEvents;

public class MapController : MonoBehaviour
{
    public static bool is_open = false;

    [Header("Map Settings")]
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 100); // Off-screen
    [SerializeField] private Vector2 visiblePosition = new Vector2(0, -1030);   // On-screen
    [SerializeField] private float slideSpeed = 2f;       // How fast progress moves 0→1
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private RectTransform mapPanel;
    private bool mapOpen = false;
    private float slideProgress = 0f; // 0 = hidden, 1 = visible

    // At the top, get the RectTransform
    RectTransform mapRect;

    void Start()
    {
        mapRect = GetComponent<RectTransform>();
        mapPanel = mapRect; // ADD THIS
        hiddenPosition = new Vector2(0, mapRect.rect.height);
        mapPanel.anchoredPosition = hiddenPosition;
    }

    void Update()
    {
        // Toggle map on tab key
        if (Input.GetKeyDown(KeyCode.Tab) && SafehouseState.paper_collected && !BuyMenuController.IsOpen)
        {
            if (HUDController.instance != null && HUDController.instance.IsEscapeOpen)
                HUDController.instance.ForceCloseEscape();

            mapOpen = !mapOpen;
            is_open = mapOpen;
            if (mapOpen) EventBus.Publish(new GameFreezeEvent() { freeze_map = false });
            else StartCoroutine(DelayedUnfreeze());
        }

        // Move progress toward target
        float targetProgress = mapOpen ? 1f : 0f;
        slideProgress = Mathf.MoveTowards(slideProgress, targetProgress, Time.unscaledDeltaTime * slideSpeed);

        // Apply to position
        if (mapPanel != null) mapPanel.anchoredPosition = Vector2.LerpUnclamped(hiddenPosition, visiblePosition, slideCurve.Evaluate(slideProgress));
    }

    IEnumerator DelayedUnfreeze()
    {
        yield return new WaitForSecondsRealtime((1f / slideSpeed)); // Wait until halfway through the slide
        EventBus.Publish(new GameUnfreezeEvent() { freeze_map = false });
    }
}