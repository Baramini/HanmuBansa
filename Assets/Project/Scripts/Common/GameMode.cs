public static class GameMode
{
    public static bool IsSingleplay { get; private set; } = false;

    public static void SetSingleplay() => IsSingleplay = true;
    public static void SetMultiplay() => IsSingleplay = false;
}
