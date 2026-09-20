using Godot;

public static class WorldMaps
{
    public const string KameHouse = "kame_house";
    public const string CleanPath = "clean_path";
    public const string BearThief01 = StageAreaIds.BearThief01;
    public const string BearThief02 = StageAreaIds.BearThief02;
    public const string BearThief03 = StageAreaIds.BearThief03;
    public const string BearThiefBoss = StageAreaIds.BearThiefBoss;

    public static readonly Rect2 KameHouseBounds = new(0, 0, 1600, 900);
    public static readonly Rect2 CleanPathBounds = new(2048, 0, 1600, 900);
    public static readonly Rect2 BearThief01Bounds = new(4096, 0, 1600, 900);
    public static readonly Rect2 BearThief02Bounds = new(6144, 0, 1600, 900);
    public static readonly Rect2 BearThief03Bounds = new(8192, 0, 1600, 900);
    public static readonly Rect2 BearThiefBossBounds = new(10240, 0, 1600, 900);

    public static bool TryGetBounds(string mapId, out Rect2 bounds)
    {
        switch (mapId)
        {
            case KameHouse:
                bounds = KameHouseBounds;
                return true;
            case CleanPath:
                bounds = CleanPathBounds;
                return true;
            case BearThief01:
                bounds = BearThief01Bounds;
                return true;
            case BearThief02:
                bounds = BearThief02Bounds;
                return true;
            case BearThief03:
                bounds = BearThief03Bounds;
                return true;
            case BearThiefBoss:
                bounds = BearThiefBossBounds;
                return true;
            default:
                bounds = default;
                return false;
        }
    }

    public static Vector2 GetArrivalPosition(string mapId)
    {
        if (!TryGetBounds(mapId, out Rect2 bounds))
            bounds = KameHouseBounds;

        if (IsBearThiefArea(mapId))
            return bounds.Position + new Vector2(140, 640);
        if (mapId == CleanPath)
            return bounds.Position + new Vector2(800, 640);
        return bounds.GetCenter();
    }

    public static bool IsBearThiefArea(string? mapId) => mapId is
        BearThief01 or BearThief02 or BearThief03 or BearThiefBoss;

    public static bool IsSelectableDestination(string? mapId) => mapId is
        KameHouse or CleanPath or BearThief01;

    public static string GetMapIdAt(Vector2 position)
    {
        if (BearThiefBossBounds.HasPoint(position))
            return BearThiefBoss;
        if (BearThief03Bounds.HasPoint(position))
            return BearThief03;
        if (BearThief02Bounds.HasPoint(position))
            return BearThief02;
        if (BearThief01Bounds.HasPoint(position))
            return BearThief01;
        if (CleanPathBounds.HasPoint(position))
            return CleanPath;
        return KameHouse;
    }

    public static Rect2 GetBoundsAt(Vector2 position) =>
        TryGetBounds(GetMapIdAt(position), out Rect2 bounds) ? bounds : KameHouseBounds;
}
