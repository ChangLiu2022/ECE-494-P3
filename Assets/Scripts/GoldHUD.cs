using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GoldHUD : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField] private int startingGold = 100;
    [SerializeField] private int pitCoinLoss = 10;

    [Header("Normal Style")]
    [SerializeField] private int normalFontSize = 28;
    [SerializeField] private Color normalColor = Color.yellow;

    [Header("Pit Flash Style")]
    [SerializeField] private int flashFontSize = 42;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private int flashCount = 3;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 size = new Vector2(200f, 50f);

    private int _gold;
    private Text _text;
    private GameObject _hudObject;
    private Coroutine _flashRoutine;

    public int Gold => _gold;

    private void Awake()
    {
        CreateHUD();
        _hudObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameEvents.GoldEvent>(OnGold);
        EventBus.Subscribe<GameEvents.VehiclePitEvent>(OnPit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameEvents.GoldEvent>(OnGold);
        EventBus.Unsubscribe<GameEvents.VehiclePitEvent>(OnPit);
    }

    private void OnGold(GameEvents.GoldEvent evt)
    {
        _gold = startingGold;
        _hudObject.SetActive(true);
        SetNormalStyle();
        UpdateText();
    }

    private void OnPit(GameEvents.VehiclePitEvent evt)
    {
        _gold = Mathf.Max(0, _gold - pitCoinLoss);
        UpdateText();

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float halfFlash = flashDuration / (flashCount * 2);

        for (int i = 0; i < flashCount; i++)
        {
            // Flash style
            _text.fontSize = flashFontSize;
            _text.color = flashColor;
            yield return new WaitForSeconds(halfFlash);

            // Normal style
            SetNormalStyle();
            yield return new WaitForSeconds(halfFlash);
        }

        SetNormalStyle();
        _flashRoutine = null;
    }

    private void SetNormalStyle()
    {
        _text.fontSize = normalFontSize;
        _text.color = normalColor;
    }

    private void UpdateText()
    {
        _text.text = $"Gold: {_gold}";
    }

    private void CreateHUD()
    {
        _hudObject = new GameObject("GoldText");
        _hudObject.transform.SetParent(transform, false);

        _text = _hudObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = normalFontSize;
        _text.color = normalColor;
        _text.alignment = TextAnchor.UpperRight;

        var rect = _text.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }
}