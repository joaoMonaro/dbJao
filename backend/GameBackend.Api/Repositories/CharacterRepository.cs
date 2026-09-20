using GameBackend.Api.Data;
using GameBackend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Repositories;

public sealed class CharacterRepository(GameDbContext dbContext) : ICharacterRepository
{
    public async Task<IReadOnlyList<Character>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Characters
            .AsNoTracking()
            .Include(character => character.CompletedStages)
            .Where(character => character.UserId == userId)
            .OrderBy(character => character.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Character?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Characters
            .AsNoTracking()
            .Include(character => character.CompletedStages)
            .SingleOrDefaultAsync(
                character => character.Id == id && character.UserId == userId,
                cancellationToken
            );
    }

    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Characters.CountAsync(
            character => character.UserId == userId,
            cancellationToken
        );
    }

    public Task<bool> NameExistsForUserAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Characters.AnyAsync(
            character => character.UserId == userId && character.Name == name,
            cancellationToken
        );
    }

    public async Task AddAsync(Character character, CancellationToken cancellationToken)
    {
        dbContext.Characters.Add(character);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateStateAsync(
        Guid characterId,
        Guid userId,
        int currentHealth,
        int level,
        long reset,
        long totalXp,
        long baseBattlePower,
        string activeCharacterId,
        IReadOnlyCollection<string> completedStages,
        string mapId,
        float positionX,
        float positionY,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        int affectedRows = await dbContext.Characters
            .Where(character =>
                character.Id == characterId
                && character.UserId == userId
                && currentHealth <= character.MaxHealth
                && character.TotalXp <= totalXp
                && character.BaseBattlePower <= baseBattlePower)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(character => character.CurrentHealth, currentHealth)
                    .SetProperty(character => character.Level, level)
                    .SetProperty(character => character.Reset, reset)
                    .SetProperty(character => character.TotalXp, totalXp)
                    .SetProperty(character => character.BaseBattlePower, baseBattlePower)
                    .SetProperty(character => character.ActiveCharacterId, activeCharacterId)
                    .SetProperty(character => character.MapId, mapId)
                    .SetProperty(character => character.PositionX, positionX)
                    .SetProperty(character => character.PositionY, positionY)
                    .SetProperty(character => character.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        if (affectedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        string[] existingStages = await dbContext.CharacterCompletedStages
            .Where(completion => completion.CharacterId == characterId)
            .Select(completion => completion.StageId)
            .ToArrayAsync(cancellationToken);
        HashSet<string> existing = new(existingStages, StringComparer.Ordinal);
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;
        foreach (string stageId in completedStages)
        {
            if (!existing.Add(stageId))
                continue;

            dbContext.CharacterCompletedStages.Add(new CharacterCompletedStage
            {
                CharacterId = characterId,
                StageId = stageId,
                CompletedAt = completedAt,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
