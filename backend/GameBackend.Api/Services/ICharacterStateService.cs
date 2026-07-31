using GameBackend.Api.DTOs.GameSessions;

namespace GameBackend.Api.Services;

public interface ICharacterStateService
{
    Task<bool> SaveAsync(
        Guid characterId,
        SaveCharacterStateRequest request,
        CancellationToken cancellationToken);
}
