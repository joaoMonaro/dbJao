using GameBackend.Api.Entities;

namespace GameBackend.Api.Repositories;

public interface ICharacterRepository
{
    Task<IReadOnlyList<Character>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<Character?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> NameExistsForUserAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken
    );

    Task AddAsync(Character character, CancellationToken cancellationToken);

    Task<bool> UpdateStateAsync(
        Guid characterId,
        Guid userId,
        int currentHealth,
        string mapId,
        float positionX,
        float positionY,
        CancellationToken cancellationToken);
}
