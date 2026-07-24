namespace GameBackend.Api.Entities;

public sealed class Character : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public int CurrentHealth { get; set; } = 100;
    public string MapId { get; set; } = "kame_house";
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
