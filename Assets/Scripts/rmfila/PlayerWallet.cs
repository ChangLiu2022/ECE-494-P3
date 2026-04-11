using static GameEvents;

public static class PlayerWallet
{
    public static int current_money { get; private set; } = 0;
    public static int current_level { get; private set; } = 1;
    public static bool level_reward_claimed { get; private set; } = false;


    public static void ClaimLevelReward()
    {
        level_reward_claimed = true;
    }


    public static void ResetLevelProgress()
    {
        level_reward_claimed = false;
    }


    public static void AdvanceLevel()
    {
        if (level_reward_claimed)
            Add(1000 * current_level);

        current_level++;
        level_reward_claimed = false;
    }

    public static void Add(int amount)
    {
        if (amount == 0) return;
        current_money += amount;

        EventBus.Publish(new MoneyChangedEvent
        {
            current_money = current_money,
            delta = amount
        });
    }

    public static void Reset()
    {
        current_money = 0;
        current_level = 1;
        level_reward_claimed = false;
        EventBus.Publish(new MoneyChangedEvent
        {
            current_money = 0,
            delta = 0
        });
    }
}
