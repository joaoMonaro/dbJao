using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Repositories;

namespace GameBackend.Api.Services;

public sealed class CharacterStateService(
    ICharacterRepository characterRepository,
    ILogger<CharacterStateService> logger) : ICharacterStateService
{
    public async Task<bool> SaveAsync(
        Guid characterId,
        SaveCharacterStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!float.IsFinite(request.PositionX) || !float.IsFinite(request.PositionY))
            return false;
        if (request.Level is < 0 or >= 200 || request.Reset < 0 || request.TotalXp < 0
            || request.BaseBattlePower < 10)
            return false;

        string activeCharacterId = request.ActiveCharacterId.Trim();
        if (string.IsNullOrWhiteSpace(activeCharacterId))
            return false;

        string mapId = request.MapId.Trim();
        if (string.IsNullOrWhiteSpace(mapId))
            return false;

        bool saved = await characterRepository.UpdateStateAsync(
            characterId,
            request.UserId,
            request.CurrentHealth,
            request.Level,
            request.Reset,
            request.TotalXp,
            request.BaseBattlePower,
            activeCharacterId,
            mapId,
            request.PositionX,
            request.PositionY,
            cancellationToken);

        if (saved)
            logger.LogInformation("[PERSISTENCE] Estado do personagem {CharacterId} salvo", characterId);
        else
            logger.LogWarning("[PERSISTENCE] Personagem {CharacterId} não encontrado", characterId);

        return saved;
    }
}
