public static class SafehouseState
{
    public static bool gun_collected = false;
    public static bool paper_collected = false;
    public static bool completed_tutorial = false;
    public static bool completed_newmap = false;
    public static bool workbench_interacted = false;
    public static bool gun_bar_activated = false;

    public static void Reset()
    {
        gun_collected = false;
        paper_collected = false;
        completed_tutorial = false;
        completed_newmap = false;
        workbench_interacted = false;
        PlayerWallet.Reset();
    }
}
