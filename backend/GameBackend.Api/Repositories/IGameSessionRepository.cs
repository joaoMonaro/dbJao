using GameBackend.Api.Entities;

namespace GameBackend.Api.Repositories;

public interface IGameSessionRepository
{
    Task<int> CountPendingByUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> HasPendingForCharacterAsync(
        Guid userId,
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task AddAsync(GameSession session, CancellationToken cancellationToken);

    Task<ConsumeGameSessionResult> TryConsumeAsync(
        string tokenHash,
        string gameServerId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public enum ConsumeGameSessionStatus
{
    Consumed,
    NotFound,
    Expired,
    AlreadyConsumed,
    CharacterNotFound,
    UserNotFound,
}

public sealed record ConsumeGameSessionResult(
    ConsumeGameSessionStatus Status,
    GameSession? Session = null);
