using GameBackend.Api.DTOs.GameSessions;

namespace GameBackend.Api.Services;

public interface IGameSessionService
{
    Task<CreateGameSessionResponse> CreateSessionAsync(
        Guid userId,
        Guid characterId,
        CancellationToken cancellationToken);

    Task<ValidateGameSessionResponse> ValidateAndConsumeAsync(
        string sessionToken,
        string gameServerId,
        CancellationToken cancellationToken);
}
