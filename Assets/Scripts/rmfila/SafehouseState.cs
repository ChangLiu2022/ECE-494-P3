public static class SafehouseState
{
    public static bool gun_collected = false;
    public static bool paper_collected = false;
    public static bool completed_tutorial = false;
    public static bool completed_newmap = false;
    public static bool workbench_interacted = false;
    public static bool reached_rifle = false;
    public static float gun_bar_mult = 1f;

    public static void Reset()
    {
        gun_collected = false;
        paper_collected = false;
        completed_tutorial = false;
        completed_newmap = false;
        workbench_interacted = false;
        reached_rifle = false;
        gun_bar_mult = 1f;
    }
}
