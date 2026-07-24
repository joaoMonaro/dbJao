using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.Configuration;

public sealed class GameServerOptions
{
    public const string SectionName = "GameServer";

    [Required]
    [MinLength(32)]
    public string InternalApiKey { get; init; } = string.Empty;

    [Required]
    public string DefaultHost { get; init; } = "127.0.0.1";

    [Range(1, 65535)]
    public int DefaultPort { get; init; } = 7000;
}
