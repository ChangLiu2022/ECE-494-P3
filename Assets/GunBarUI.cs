using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameEvents;

public class GunBarUI : MonoBehaviour
{
    public GunBarController gunBar;
    public Image fillImage;
    public Image rootObject;
    public Image upgrade;
    public Image downgrade;

    [Header("Sprites")]
    [SerializeField] Sprite pistol_icon;
    [SerializeField] Sprite shotgun_icon;
    [SerializeField] Sprite rifle_icon;

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
        upgrade.sprite = shotgun_icon;
        downgrade.sprite = pistol_icon;

        if(SceneManager.GetActiveScene().name == "Safehouse" && !SafehouseState.gun_collected)
        {
            rootObject.enabled = false;
            upgrade.enabled = false;
            downgrade.enabled = false;
        }
    }

    void OnFirstHit(FirstHitEvent e)
    {
        hasActivated = true;
        rootObject.enabled = true;
        upgrade.enabled = true;
        downgrade.enabled = true;
    }

    void Update()
    {
        if (!hasActivated) return;

        fillImage.fillAmount = gunBar.displayedProgress;
    }
}