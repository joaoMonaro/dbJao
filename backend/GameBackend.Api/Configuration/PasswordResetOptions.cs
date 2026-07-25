using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.Configuration;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    [Range(5, 1440)]
    public int ExpirationMinutes { get; init; } = 30;
    public bool ExposeTokenInDevelopment { get; init; }
    public string DevelopmentOutputDirectory { get; init; } = "/tmp/dbjao-password-reset";
}
