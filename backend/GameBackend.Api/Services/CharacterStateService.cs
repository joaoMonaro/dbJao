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

        IReadOnlyList<string> requestedStages = request.CompletedStages ?? [];
        if (requestedStages.Count > 256)
            return false;
        string[] completedStages = requestedStages
            .Select(stageId => stageId?.Trim() ?? string.Empty)
            .ToArray();
        if (completedStages.Any(stageId => !IsValidStageId(stageId)))
            return false;
        completedStages = completedStages
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

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
            completedStages,
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

    private static bool IsValidStageId(string stageId)
    {
        if (stageId.Length is < 1 or > 64)
            return false;

        return stageId.All(character => character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character == '_');
    }
}
