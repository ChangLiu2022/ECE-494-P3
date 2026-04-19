using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite speakerSprite;
    [TextArea(2, 5)]
    public string text;
    public UnityEvent onShow;
}

/// <summary>
/// In-game dialogue cutscene using TextMeshPro for emoji/rich text support.
/// Shows a dialogue box at the bottom with character sprite and text.
/// Left click to advance. Freezes gameplay during cutscene.
/// Attach to a Canvas.
///
/// For emojis: create a TMP Sprite Asset, assign it to the TMP components,
/// then use <sprite name="emoji_name"> in your dialogue text.
/// </summary>
public class DialogueCutscene : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private List<DialogueLine> lines;

    [Header("UI Style")]
    [SerializeField] private Color boxColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private Color nameColor = Color.yellow;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float nameFontSize = 24f;
    [SerializeField] private float textFontSize = 20f;
    [SerializeField] private float boxHeight = 200f;
    [SerializeField] private float spriteSize = 150f;
    [SerializeField] private float padding = 20f;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private TMP_SpriteAsset spriteAsset;

    [Header("Flow")]
    [SerializeField] private string nextSceneName;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    private GameObject _dialoguePanel;
    private Image _speakerImage;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _bodyText;
    private int _currentLine;
    private bool _active;
    private Image _fadeImage;

    private void Start()
    {
        CreateUI();
        _dialoguePanel.SetActive(false);
        StartDialogue();
    }

    public void StartDialogue()
    {
        if (lines == null || lines.Count == 0) return;

        _currentLine = 0;
        _active = true;
        _dialoguePanel.SetActive(true);
        Time.timeScale = 0f;

        ShowLine(_currentLine);
    }

    private void Update()
    {
        if (!_active) return;

        if (Input.GetMouseButtonDown(0))
        {
            _currentLine++;

            if (_currentLine >= lines.Count)
            {
                EndDialogue();
                return;
            }

            ShowLine(_currentLine);
        }
    }

    private void ShowLine(int index)
    {
        var line = lines[index];

        _nameText.text = line.speakerName;
        _bodyText.text = line.text;

        if (line.speakerSprite != null)
        {
            _speakerImage.sprite = line.speakerSprite;
            _speakerImage.gameObject.SetActive(true);
        }
        else
        {
            _speakerImage.gameObject.SetActive(false);
        }

        line.onShow?.Invoke();
    }

    private void EndDialogue()
    {
        _active = false;
        _dialoguePanel.SetActive(false);
        Time.timeScale = 1f;

        if (FadeManager.Instance != null)
        {
            float fadeDuration;
            if (nextSceneName == "Safehouse" || nextSceneName.StartsWith("CutScene")) fadeDuration = 1.95f;
            else fadeDuration = 2f;

            FadeManager.Instance.StartTransition(nextSceneName, null, fadeDuration);
        }
        else
        {
            StartCoroutine(FadeAndTransition());
        }
    }

    private System.Collections.IEnumerator FadeAndTransition()
    {
        var fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(transform, false);

        _fadeImage = fadeObj.AddComponent<Image>();
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        _fadeImage.raycastTarget = false;

        var rect = fadeObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        _fadeImage.color = fadeColor;

        if (!string.IsNullOrEmpty(nextSceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private void CreateUI()
    {
        // Panel
        _dialoguePanel = new GameObject("DialoguePanel");
        _dialoguePanel.transform.SetParent(transform, false);

        var panelImage = _dialoguePanel.AddComponent<Image>();
        panelImage.color = boxColor;

        var panelRect = _dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, boxHeight);

        // Speaker sprite
        var spriteObj = new GameObject("SpeakerSprite");
        spriteObj.transform.SetParent(_dialoguePanel.transform, false);

        _speakerImage = spriteObj.AddComponent<Image>();
        _speakerImage.preserveAspect = true;

        var spriteRect = spriteObj.GetComponent<RectTransform>();
        spriteRect.anchorMin = new Vector2(0f, 0f);
        spriteRect.anchorMax = new Vector2(0f, 1f);
        spriteRect.pivot = new Vector2(0f, 0f);
        spriteRect.anchoredPosition = new Vector2(padding, 0f);
        spriteRect.sizeDelta = new Vector2(spriteSize, 0f);

        // Name text
        var nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(_dialoguePanel.transform, false);

        _nameText = nameObj.AddComponent<TextMeshProUGUI>();
        if (font != null) _nameText.font = font;
        if (spriteAsset != null) _nameText.spriteAsset = spriteAsset;
        _nameText.fontSize = nameFontSize;
        _nameText.color = nameColor;
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.alignment = TextAlignmentOptions.TopLeft;
        _nameText.richText = true;

        var nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(padding + spriteSize + padding, -padding);
        nameRect.sizeDelta = new Vector2(-(padding * 3 + spriteSize), 30f);

        // Body text
        var bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(_dialoguePanel.transform, false);

        _bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        if (font != null) _bodyText.font = font;
        if (spriteAsset != null) _bodyText.spriteAsset = spriteAsset;
        _bodyText.fontSize = textFontSize;
        _bodyText.color = textColor;
        _bodyText.alignment = TextAlignmentOptions.TopLeft;
        _bodyText.richText = true;

        var bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(padding + spriteSize + padding, padding);
        bodyRect.offsetMax = new Vector2(-padding, -(padding + 35f));

        // Click to continue prompt
        var promptObj = new GameObject("ContinuePrompt");
        promptObj.transform.SetParent(_dialoguePanel.transform, false);

        var promptText = promptObj.AddComponent<TextMeshProUGUI>();
        if (font != null) promptText.font = font;
        promptText.fontSize = textFontSize * 0.7f;
        promptText.color = new Color(textColor.r, textColor.g, textColor.b, 0.6f);
        promptText.text = "Click to continue...";
        promptText.alignment = TextAlignmentOptions.BottomRight;
        promptText.fontStyle = FontStyles.Italic;

        var promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(1f, 0f);
        promptRect.anchoredPosition = new Vector2(-padding, padding * 0.5f);
        promptRect.sizeDelta = new Vector2(200f, 20f);
    }
}