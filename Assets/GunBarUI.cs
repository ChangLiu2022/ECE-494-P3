using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameEvents;

public class GunBarUI : MonoBehaviour
{
    public GunBarController gunBar;
    public Image fillImage;
    public Image rootObject;

    private bool hasActivated = false;

    void OnEnable()
    {
        EventBus.Subscribe<FirstHitEvent>(OnFirstHit);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FirstHitEvent>(OnFirstHit);
    }

    private void Start()
    {
        Color c = rootObject.color;
        c.a = 0;
        rootObject.color = c;
    }

    void OnFirstHit(FirstHitEvent e)
    {
        hasActivated = true;
        Color c = rootObject.color;
        c.a = 1;
        rootObject.color = c;
    }

    void Update()
    {
        if (!hasActivated) return;

        fillImage.fillAmount = gunBar.displayedProgress;
    }
}