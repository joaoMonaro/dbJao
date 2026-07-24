namespace GameBackend.Api.Entities;

public sealed class GameSession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid CharacterId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public bool IsConsumed { get; set; }
    public string? GameServerId { get; set; }
    public User User { get; set; } = null!;
    public Character Character { get; set; } = null!;
}
