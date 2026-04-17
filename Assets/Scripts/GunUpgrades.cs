using static GameEvents;

public static class GunUpgrades
{
    public enum Weapon { Pistol = 0, Shotgun = 1, Rifle = 2 }
    public enum Track { Damage = 0, FireRate = 1, Cooldown = 2 }

    public const int MaxLevel = 4;

    // [weapon, track]
    private static readonly int[,] levels = new int[3, 3];

    public static int GetLevel(Weapon w, Track t) => levels[(int)w, (int)t];

    // +25% damage per level (level 4 = 2x damage)
    public static float GetDamageMultiplier(Weapon w)
        => 1f + 0.25f * levels[(int)w, (int)Track.Damage];

    // +25% fire rate per level — divide fire interval by this
    public static float GetFireRateMultiplier(Weapon w)
        => 1f + 0.25f * levels[(int)w, (int)Track.FireRate];

    // +25% longer decay delay per level — multiply decayDelay by this
    public static float GetCooldownMultiplier(Weapon w)
        => 1f + 0.25f * levels[(int)w, (int)Track.Cooldown];

    // Base cost per weapon tier; scales with next level being purchased
    private static readonly int[] base_cost = { 250, 500, 750 };

    /// <summary>Returns cost for next purchase, or -1 if already maxed.</summary>
    public static int GetCost(Weapon w, Track t)
    {
        int current = levels[(int)w, (int)t];
        if (current >= MaxLevel) return -1;
        return base_cost[(int)w] * (current + 1);
    }

    public static bool TryPurchase(Weapon w, Track t)
    {
        if (levels[(int)w, (int)t] >= MaxLevel) return false;
        int cost = GetCost(w, t);
        if (!PlayerWallet.TrySpend(cost)) return false;
        levels[(int)w, (int)t]++;
        EventBus.Publish(new UpgradePurchasedEvent
        {
            weapon = w,
            track = t,
            new_level = levels[(int)w, (int)t]
        });
        return true;
    }

    public static void Reset()
    {
        for (int w = 0; w < 3; w++)
            for (int t = 0; t < 3; t++)
                levels[w, t] = 0;
    }
}
