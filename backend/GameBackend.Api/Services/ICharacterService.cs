using GameBackend.Api.DTOs.Characters;

namespace GameBackend.Api.Services;

public interface ICharacterService
{
    Task<IReadOnlyList<CharacterResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<CharacterResponse> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    );

    Task<CharacterResponse> CreateAsync(
        Guid userId,
        CreateCharacterRequest request,
        CancellationToken cancellationToken
    );
}
