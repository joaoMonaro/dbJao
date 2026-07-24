using GameBackend.Api.Authentication;
using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameBackend.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("internal-game-server")]
[Route("api/internal/game-sessions")]
public sealed class InternalGameSessionsController(
    IGameSessionService gameSessionService,
    IGameServerKeyValidator keyValidator,
    ILogger<InternalGameSessionsController> logger) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidateGameSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidateGameSessionResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ValidateGameSessionResponse>> Validate(
        [FromBody] ValidateGameSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!keyValidator.IsValid(Request.Headers["X-Game-Server-Key"].FirstOrDefault()))
        {
            logger.LogWarning("[GAME SESSION] Servidor não autorizado");
            return Unauthorized(new ValidateGameSessionResponse(false, "INVALID_SERVER_KEY"));
        }

        return Ok(await gameSessionService.ValidateAndConsumeAsync(
            request.SessionToken,
            request.GameServerId,
            cancellationToken));
    }
}
