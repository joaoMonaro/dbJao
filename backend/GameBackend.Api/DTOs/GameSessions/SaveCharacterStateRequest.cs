using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.GameSessions;

public sealed record SaveCharacterStateRequest(
    [Required] Guid UserId,
    [Range(0, int.MaxValue)] int CurrentHealth,
    [Range(0, 199)] int Level,
    [Range(0, long.MaxValue)] long Reset,
    [Range(0, long.MaxValue)] long TotalXp,
    [Range(10, long.MaxValue)] long BaseBattlePower,
    [Required, MaxLength(64)] string MapId,
    float PositionX,
    float PositionY
);
