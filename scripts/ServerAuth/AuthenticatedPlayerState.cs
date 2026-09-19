using System;

public sealed record AuthenticatedPlayerState(
    Guid UserId,
    Guid CharacterId,
    int CurrentHealth,
    int Level,
    long Reset,
    long TotalXp,
    string MapId,
    float PositionX,
    float PositionY);
