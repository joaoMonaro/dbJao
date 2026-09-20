public enum StageEntrySide
{
    Left,
    Right,
}

public readonly record struct StageTransition(string TargetAreaId, StageEntrySide EntrySide);

public static class StageAreaIds
{
    public const string BearThief01 = "bear_thief_01";
    public const string BearThief02 = "bear_thief_02";
    public const string BearThief03 = "bear_thief_03";
    public const string BearThiefBoss = "bear_thief_boss";
}

public static class StageRoute
{
    public static bool TryGetTransition(
        string areaId,
        int horizontalDirection,
        out StageTransition transition)
    {
        transition = default;
        if (horizontalDirection == 0)
            return false;

        string? target = (areaId, horizontalDirection > 0) switch
        {
            (StageAreaIds.BearThief01, true) => StageAreaIds.BearThief02,
            (StageAreaIds.BearThief02, true) => StageAreaIds.BearThief03,
            (StageAreaIds.BearThief03, true) => StageAreaIds.BearThiefBoss,
            (StageAreaIds.BearThief02, false) => StageAreaIds.BearThief01,
            (StageAreaIds.BearThief03, false) => StageAreaIds.BearThief02,
            (StageAreaIds.BearThiefBoss, false) => StageAreaIds.BearThief03,
            _ => null,
        };

        if (target is null)
            return false;

        transition = new(
            target,
            horizontalDirection > 0 ? StageEntrySide.Left : StageEntrySide.Right);
        return true;
    }
}
