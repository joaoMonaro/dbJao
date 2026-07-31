using GameBackend.Api.Authentication;
using GameBackend.Api.DTOs.Characters;
using GameBackend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/characters")]
public sealed class CharactersController(ICharacterService characterService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<CharacterResponse>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CharacterResponse>>> GetAll(
        CancellationToken cancellationToken
    )
    {
        Guid userId = User.GetRequiredUserId();
        return Ok(await characterService.GetAllAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CharacterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CharacterResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        Guid userId = User.GetRequiredUserId();
        return Ok(
            await characterService.GetByIdAsync(id, userId, cancellationToken)
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(CharacterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CharacterResponse>> Create(
        [FromBody] CreateCharacterRequest request,
        CancellationToken cancellationToken
    )
    {
        Guid userId = User.GetRequiredUserId();
        CharacterResponse response = await characterService.CreateAsync(
            userId,
            request,
            cancellationToken
        );

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
