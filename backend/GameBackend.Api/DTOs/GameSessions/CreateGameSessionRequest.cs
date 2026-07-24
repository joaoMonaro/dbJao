using System.ComponentModel.DataAnnotations;

namespace GameBackend.Api.DTOs.GameSessions;

public sealed record CreateGameSessionRequest([Required] Guid CharacterId);
