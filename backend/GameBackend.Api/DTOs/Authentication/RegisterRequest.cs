using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.Authentication;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(72, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
