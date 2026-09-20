namespace GameBackend.Api.Entities;

public sealed class CharacterCompletedStage
{
    public Guid CharacterId { get; set; }
    public string StageId { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }
    public Character Character { get; set; } = null!;
}
