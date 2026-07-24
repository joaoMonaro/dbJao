using GameBackend.Api.Authentication;
using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameBackend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/game-sessions")]
public sealed class GameSessionsController(IGameSessionService gameSessionService)
    : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("game-session-create")]
    [ProducesResponseType(typeof(CreateGameSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreateGameSessionResponse>> Create(
        [FromBody] CreateGameSessionRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = User.GetRequiredUserId();
        CreateGameSessionResponse response = await gameSessionService.CreateSessionAsync(
            userId,
            request.CharacterId,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
