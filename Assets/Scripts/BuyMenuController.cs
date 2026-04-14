using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameEvents;

public class BuyMenuController : MonoBehaviour
{
    public static BuyMenuController instance { get; private set; }
    public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text money_text;

    // 9 slots: index = weapon*3 + track
    // 0-2 = Pistol   (Damage, FireRate, Cooldown)
    // 3-5 = Shotgun  (Damage, FireRate, Cooldown)
    // 6-8 = Rifle    (Damage, FireRate, Cooldown)
    [Header("Cell Level Texts  [weapon*3 + track]")]
    [SerializeField] private TMP_Text[] level_texts = new TMP_Text[9];

    [Header("Cell Cost Texts  [weapon*3 + track]")]
    [SerializeField] private TMP_Text[] cost_texts = new TMP_Text[9];

    [Header("Cell Buttons  [weapon*3 + track]")]
    [SerializeField] private Button[] buy_buttons = new Button[9];


    private void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<MoneyChangedEvent>(OnMoneyChanged);
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MoneyChangedEvent>(OnMoneyChanged);
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }


    public void Open()
    {
        if (IsOpen) return;
        panel.SetActive(true);
        RefreshAll();
        EventBus.Publish(new GameFreezeEvent());
    }

    // Wire to your Close button's OnClick
    public void Close()
    {
        panel.SetActive(false);
        EventBus.Publish(new GameUnfreezeEvent());
    }

    public void OnBuyClicked(int index)
    {
        var weapon = (GunUpgrades.Weapon)(index / 3);
        var track = (GunUpgrades.Track)(index % 3);
        GunUpgrades.TryPurchase(weapon, track);
    }


    private void OnMoneyChanged(MoneyChangedEvent e) => RefreshAll();
    private void OnUpgradePurchased(UpgradePurchasedEvent e) => RefreshAll();

    private void RefreshAll()
    {
        if (money_text != null)
            money_text.text = "$" + PlayerWallet.current_money;

        for (int w = 0; w < 3; w++)
        {
            var weapon = (GunUpgrades.Weapon)w;
            for (int t = 0; t < 3; t++)
            {
                var track = (GunUpgrades.Track)t;
                int i = w * 3 + t;

                int lvl = GunUpgrades.GetLevel(weapon, track);
                int cost = GunUpgrades.GetCost(weapon, track);

                if (level_texts[i] != null)
                    level_texts[i].text = "+" + (lvl * 25) + "%";

                if (cost_texts[i] != null)
                    cost_texts[i].text = cost < 0 ? "MAXED" : "$" + cost;

                if (buy_buttons[i] != null)
                    buy_buttons[i].interactable = cost >= 0 && PlayerWallet.current_money >= cost;
            }
        }
    }
}
