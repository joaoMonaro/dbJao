using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.Characters;

public sealed class CreateCharacterRequest
{
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
}
