using Godot;
using System.Collections.Generic;

public partial class BearThiefStage : Node2D
{
    private readonly Dictionary<string, StageArea> _areas = new();

    public override void _Ready()
    {
        _areas.Clear();
        foreach (Node child in GetChildren())
        {
            if (child is not StageArea area)
                continue;
            if (string.IsNullOrWhiteSpace(area.AreaId) || !_areas.TryAdd(area.AreaId, area))
            {
                GD.PushError($"[STAGE] AreaId vazio ou duplicado: '{area.AreaId}'.");
            }
        }

        if (_areas.Count != 4)
            GD.PushError($"[STAGE] Bear Thief exige quatro áreas; encontradas {_areas.Count}.");
    }

    public bool TryGetEntryPosition(
        string areaId,
        StageEntrySide side,
        out Vector2 position)
    {
        if (_areas.TryGetValue(areaId, out StageArea? area))
        {
            position = area.GetEntryPosition(side);
            return true;
        }

        position = default;
        return false;
    }
}
