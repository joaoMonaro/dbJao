using GameBackend.Api.DTOs.Characters;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using GameBackend.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameBackend.Api.Services;

public sealed class CharacterService(
    ICharacterRepository characterRepository,
    ILogger<CharacterService> logger
) : ICharacterService
{
    private const int MaximumCharactersPerUser = 5;

    public async Task<IReadOnlyList<CharacterResponse>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Character> characters =
            await characterRepository.GetAllByUserIdAsync(userId, cancellationToken);

        return characters.Select(CharacterResponse.FromEntity).ToArray();
    }

    public async Task<CharacterResponse> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        Character? character = await characterRepository.GetByIdForUserAsync(
            id,
            userId,
            cancellationToken
        );

        if (character is null)
            throw new NotFoundApiException("Personagem não encontrado.");

        return CharacterResponse.FromEntity(character);
    }

    public async Task<CharacterResponse> CreateAsync(
        Guid userId,
        CreateCharacterRequest request,
        CancellationToken cancellationToken
    )
    {
        string name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ApiValidationException("Nome do personagem é obrigatório.");

        int characterCount = await characterRepository.CountByUserIdAsync(
            userId,
            cancellationToken
        );

        if (characterCount >= MaximumCharactersPerUser)
        {
            throw new ApiValidationException(
                $"O limite de {MaximumCharactersPerUser} personagens foi atingido."
            );
        }

        if (
            await characterRepository.NameExistsForUserAsync(
                userId,
                name,
                cancellationToken
            )
        )
        {
            throw new ApiValidationException(
                "Já existe um personagem com esse nome para este usuário."
            );
        }

        Character character = new()
        {
            UserId = userId,
            Name = name,
        };

        try
        {
            await characterRepository.AddAsync(character, cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ApiValidationException(
                "Já existe um personagem com esse nome para este usuário."
            );
        }

        logger.LogInformation(
            "Personagem {CharacterId} ({CharacterName}) criado pelo usuário {UserId}",
            character.Id,
            character.Name,
            userId
        );

        return CharacterResponse.FromEntity(character);
    }
}
