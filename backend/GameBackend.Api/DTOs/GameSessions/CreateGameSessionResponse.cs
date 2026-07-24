namespace GameBackend.Api.DTOs.GameSessions;

public sealed record CreateGameSessionResponse(
    string SessionToken,
    DateTimeOffset ExpiresAt,
    Guid CharacterId,
    string GameServerHost,
    int GameServerPort
);
