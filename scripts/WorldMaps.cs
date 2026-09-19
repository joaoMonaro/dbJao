using Godot;

public static class WorldMaps
{
    public const string KameHouse = "kame_house";
    public const string CleanPath = "clean_path";

    public static readonly Rect2 KameHouseBounds = new(0, 0, 1600, 900);
    public static readonly Rect2 CleanPathBounds = new(2048, 0, 1600, 900);

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
            default:
                bounds = default;
                return false;
        }
    }

    public static Vector2 GetArrivalPosition(string mapId) =>
        mapId == CleanPath
            ? CleanPathBounds.Position + new Vector2(800, 640)
            : KameHouseBounds.GetCenter();

    public static Rect2 GetBoundsAt(Vector2 position) =>
        CleanPathBounds.HasPoint(position) ? CleanPathBounds : KameHouseBounds;
}
