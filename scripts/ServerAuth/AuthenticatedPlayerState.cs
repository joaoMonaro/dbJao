using System;

public sealed record AuthenticatedPlayerState(
    Guid UserId,
    Guid CharacterId,
    int CurrentHealth,
    string MapId,
    float PositionX,
    float PositionY);
