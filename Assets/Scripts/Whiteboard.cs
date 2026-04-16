using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static GameEvents;

public class Whiteboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform board;
    [SerializeField] RectTransform hiddenAnchor;
    [SerializeField] RectTransform shownAnchor;

    [Header("Image / Sprites")]
    [SerializeField] Image boardImage;
    [SerializeField] Sprite sprite0;
    [SerializeField] Sprite sprite1;
    [SerializeField] Sprite sprite2;

    [Header("Animation")]
    [SerializeField] float slideSpeed = 1f;
    [SerializeField] AnimationCurve moveCurve;

    public static bool IsOpen { get; private set; }

    private Vector2 hiddenPos;
    private Vector2 shownPos;

    private bool isShown = false;
    private float slideProgress = 0f;
    private Coroutine currentRoutine;

    void Start()
    {
        hiddenPos = hiddenAnchor.anchoredPosition;
        shownPos = shownAnchor.anchoredPosition;

        board.anchoredPosition = hiddenPos;
        slideProgress = 0f;

        // Auto-find Image if not assigned
        if (boardImage == null)
            boardImage = board.GetComponent<Image>();

        UpdateSprite();
    }

    public void ToggleBoard()
    {
        isShown = !isShown;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(AnimateBoard(isShown));
    }

    IEnumerator AnimateBoard(bool show)
    {
        IsOpen = show;

        if (show)
        {
            EventBus.Publish(new GameFreezeEvent() { freeze_map = true });
            UpdateSprite(); // refresh when opened
        }

        float targetProgress = show ? 1f : 0f;

        while (!Mathf.Approximately(slideProgress, targetProgress))
        {
            slideProgress = Mathf.MoveTowards(
                slideProgress,
                targetProgress,
                slideSpeed * Time.unscaledDeltaTime
            );

            float curveValue = moveCurve.Evaluate(slideProgress);

            board.anchoredPosition = Vector2.LerpUnclamped(
                hiddenPos,
                shownPos,
                curveValue
            );

            yield return null;
        }

        board.anchoredPosition = show ? shownPos : hiddenPos;

        if (!show)
            EventBus.Publish(new GameUnfreezeEvent() { freeze_map = true });

        currentRoutine = null;
    }

    private void UpdateSprite()
    {
        if (boardImage == null) return;

        // Priority: sprite2 > sprite1 > sprite0
        if (SafehouseState.completed_newmap)
        {
            boardImage.sprite = sprite2;
        }
        else if (SafehouseState.completed_tutorial)
        {
            boardImage.sprite = sprite1;
        }
        else
        {
            boardImage.sprite = sprite0;
        }
    }
}