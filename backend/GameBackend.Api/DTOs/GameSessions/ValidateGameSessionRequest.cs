using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.GameSessions;

public sealed record ValidateGameSessionRequest(
    [Required, MaxLength(512)] string SessionToken,
    [Required, MaxLength(64)] string GameServerId
);
