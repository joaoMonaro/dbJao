using GameBackend.Api.Entities;

namespace GameBackend.Api.DTOs.Characters;

public sealed record CharacterResponse(
    Guid Id,
    string Name,
    int Level,
    long Reset,
    long TotalXp,
    long BaseBattlePower,
    string ActiveCharacterId,
    IReadOnlyList<string> CompletedStages,
    int CurrentHealth,
    string MapId,
    float PositionX,
    float PositionY,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public static CharacterResponse FromEntity(Character character)
    {
        return new CharacterResponse(
            character.Id,
            character.Name,
            character.Level,
            character.Reset,
            character.TotalXp,
            character.BaseBattlePower,
            character.ActiveCharacterId,
            character.CompletedStages.Select(completion => completion.StageId)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            character.CurrentHealth,
            character.MapId,
            character.PositionX,
            character.PositionY,
            character.CreatedAt,
            character.UpdatedAt
        );
    }
}
