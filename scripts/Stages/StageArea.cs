using Godot;
using System;

public partial class StageArea : Node2D
{
    [Export] public string AreaId { get; set; } = string.Empty;

    public Vector2 LeftEntryPosition =>
        GetRequiredMarker("LeftEntrySpawnPoint").GlobalPosition;

    public Vector2 RightEntryPosition =>
        GetRequiredMarker("RightEntrySpawnPoint").GlobalPosition;

    public Vector2 GetEntryPosition(StageEntrySide side) =>
        side == StageEntrySide.Left ? LeftEntryPosition : RightEntryPosition;

    public override void _Ready()
    {
        if (!WorldMaps.TryGetBounds(AreaId, out Rect2 expectedBounds))
        {
            GD.PushError($"[STAGE] Área desconhecida em {GetPath()}: '{AreaId}'.");
            return;
        }

        if (!Position.IsEqualApprox(expectedBounds.Position))
        {
            GD.PushError(
                $"[STAGE] Origem de {AreaId} diverge de WorldMaps: "
                + $"{Position} != {expectedBounds.Position}.");
        }

        _ = LeftEntryPosition;
        _ = RightEntryPosition;
    }

    private Marker2D GetRequiredMarker(string nodeName) =>
        GetNodeOrNull<Marker2D>(nodeName)
        ?? throw new InvalidOperationException(
            $"Spawn point obrigatório ausente em {GetPath()}: {nodeName}.");
}
