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
[Route("api/internal/characters")]
public sealed class InternalCharactersController(
    ICharacterStateService characterStateService,
    IGameServerKeyValidator keyValidator) : ControllerBase
{
    [HttpPut("{characterId:guid}/state")]
    public async Task<IActionResult> SaveState(
        Guid characterId,
        [FromBody] SaveCharacterStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!keyValidator.IsValid(Request.Headers["X-Game-Server-Key"].FirstOrDefault()))
            return Unauthorized();

        bool saved = await characterStateService.SaveAsync(
            characterId,
            request,
            cancellationToken);
        return saved ? NoContent() : NotFound();
    }
}
