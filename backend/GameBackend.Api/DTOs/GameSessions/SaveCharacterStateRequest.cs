using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.GameSessions;

public sealed record SaveCharacterStateRequest(
    [Required] Guid UserId,
    [Range(0, int.MaxValue)] int CurrentHealth,
    [Required, MaxLength(64)] string MapId,
    float PositionX,
    float PositionY
);
