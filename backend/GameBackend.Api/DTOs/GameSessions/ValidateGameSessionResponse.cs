namespace GameBackend.Api.DTOs.GameSessions;

public sealed record ValidateGameSessionResponse(
    bool Valid,
    string? ErrorCode = null,
    Guid? UserId = null,
    Guid? CharacterId = null,
    string? Username = null,
    string? CharacterName = null,
    int? Level = null,
    long? Experience = null,
    int? CurrentHealth = null,
    int? MaxHealth = null,
    string? MapId = null,
    float? PositionX = null,
    float? PositionY = null
);
