using GameBackend.Api.Data;
using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Repositories;

public sealed class GameSessionRepository(GameDbContext dbContext)
    : IGameSessionRepository
{
    public Task<int> CountPendingByUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return dbContext.GameSessions.CountAsync(
            session =>
                session.UserId == userId
                && !session.IsConsumed
                && session.ExpiresAt > now,
            cancellationToken);
    }

    public Task<bool> HasPendingForCharacterAsync(
        Guid userId,
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return dbContext.GameSessions.AnyAsync(
            session =>
                session.UserId == userId
                && session.CharacterId == characterId
                && !session.IsConsumed
                && session.ExpiresAt > now,
            cancellationToken);
    }

    public async Task AddAsync(GameSession session, CancellationToken cancellationToken)
    {
        dbContext.GameSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConsumeGameSessionResult> TryConsumeAsync(
        string tokenHash,
        string gameServerId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        int affectedRows = await dbContext.GameSessions
            .Where(session =>
                session.TokenHash == tokenHash
                && !session.IsConsumed
                && session.ExpiresAt > now)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(session => session.IsConsumed, true)
                    .SetProperty(session => session.ConsumedAt, now)
                    .SetProperty(session => session.GameServerId, gameServerId),
                cancellationToken);

        if (affectedRows == 1)
        {
            GameSession? consumedSession = await dbContext.GameSessions
                .AsNoTracking()
                .Include(session => session.User)
                .Include(session => session.Character)
                .SingleOrDefaultAsync(
                    session => session.TokenHash == tokenHash,
                    cancellationToken);

            if (consumedSession?.Character is null)
                return new(ConsumeGameSessionStatus.CharacterNotFound);
            if (consumedSession.User is null)
                return new(ConsumeGameSessionStatus.UserNotFound);

            return new(ConsumeGameSessionStatus.Consumed, consumedSession);
        }

        GameSession? existing = await dbContext.GameSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                session => session.TokenHash == tokenHash,
                cancellationToken);

        if (existing is null)
            return new(ConsumeGameSessionStatus.NotFound);
        if (existing.IsConsumed)
            return new(ConsumeGameSessionStatus.AlreadyConsumed);
        if (existing.ExpiresAt <= now)
            return new(ConsumeGameSessionStatus.Expired);

        return new(ConsumeGameSessionStatus.NotFound);
    }
}
