using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.Configuration;

public sealed class GameSessionOptions
{
    public const string SectionName = "GameSession";

    [Range(10, 600)]
    public int ExpirationSeconds { get; init; } = 60;

    [Range(1, 20)]
    public int MaxPendingSessionsPerUser { get; init; } = 3;
}
