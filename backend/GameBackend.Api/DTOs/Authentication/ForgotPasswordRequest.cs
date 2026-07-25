using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.Authentication;

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
}
