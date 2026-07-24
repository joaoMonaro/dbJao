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
}
