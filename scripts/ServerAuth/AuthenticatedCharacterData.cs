using System;

public sealed class AuthenticatedCharacterData
{
    public Guid UserId { get; init; }
    public Guid CharacterId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public int Level { get; init; }
    public long Experience { get; init; }
    public int CurrentHealth { get; init; }
    public int MaxHealth { get; init; }
    public string MapId { get; init; } = string.Empty;
    public float PositionX { get; init; }
    public float PositionY { get; init; }
}
