using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.Authentication;

public sealed class ResetPasswordRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
    [Required, StringLength(256)]
    public string Token { get; init; } = string.Empty;
    [Required, StringLength(72, MinimumLength = 8)]
    public string NewPassword { get; init; } = string.Empty;
}
