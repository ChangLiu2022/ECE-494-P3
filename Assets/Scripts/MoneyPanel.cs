using TMPro;
using UnityEngine;
using static GameEvents;


public class MoneyPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text money_text;
    [SerializeField] private string format = "Money: ${0}";

    private void OnEnable()
    {
        EventBus.Subscribe<MoneyChangedEvent>(OnMoneyChanged);
        Refresh(PlayerWallet.current_money);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MoneyChangedEvent>(OnMoneyChanged);
    }

    private void OnMoneyChanged(MoneyChangedEvent e)
    {
        Refresh(e.current_money);
    }

    private void Refresh(int amount)
    {
        if (money_text != null)
            money_text.text = string.Format(format, amount);
    }
}
