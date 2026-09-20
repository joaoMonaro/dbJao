using System;
using System.Collections.Generic;

public sealed record AuthenticatedPlayerState(
    Guid UserId,
    Guid CharacterId,
    int CurrentHealth,
    int Level,
    long Reset,
    long TotalXp,
    long BaseBattlePower,
    string ActiveCharacterId,
    IReadOnlyList<string> CompletedStages,
    string MapId,
    float PositionX,
    float PositionY);
