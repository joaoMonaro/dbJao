using GameBackend.Api.Authentication;
using GameBackend.Api.Configuration;
using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using GameBackend.Api.Repositories;
using Microsoft.Extensions.Options;

namespace GameBackend.Api.Services;

public sealed class GameSessionService(
    IUserRepository userRepository,
    ICharacterRepository characterRepository,
    IGameSessionRepository gameSessionRepository,
    IGameSessionTokenProtector tokenProtector,
    IOptions<GameSessionOptions> sessionOptions,
    IOptions<GameServerOptions> serverOptions,
    TimeProvider timeProvider,
    ILogger<GameSessionService> logger) : IGameSessionService
{
    private readonly GameSessionOptions _sessionOptions = sessionOptions.Value;
    private readonly GameServerOptions _serverOptions = serverOptions.Value;

    public async Task<CreateGameSessionResponse> CreateSessionAsync(
        Guid userId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new UnauthorizedApiException("Usuário autenticado não encontrado.");
        if (user.IsBlocked)
            throw new UnauthorizedApiException("Usuário bloqueado.");

        Character? character = await characterRepository.GetByIdForUserAsync(
            characterId,
            userId,
            cancellationToken);
        if (character is null)
            throw new NotFoundApiException("Personagem não encontrado.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (await gameSessionRepository.HasPendingForCharacterAsync(
                userId,
                characterId,
                now,
                cancellationToken))
        {
            throw new ApiValidationException(
                "Já existe uma sessão pendente válida para este personagem.");
        }

        int pendingSessions = await gameSessionRepository.CountPendingByUserAsync(
            userId,
            now,
            cancellationToken);
        if (pendingSessions >= _sessionOptions.MaxPendingSessionsPerUser)
            throw new ApiValidationException("Limite de sessões pendentes atingido.");

        string token = tokenProtector.GenerateToken();
        DateTimeOffset expiresAt = now.AddSeconds(_sessionOptions.ExpirationSeconds);
        GameSession session = new()
        {
            UserId = userId,
            CharacterId = characterId,
            TokenHash = tokenProtector.ComputeHash(token),
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

        await gameSessionRepository.AddAsync(session, cancellationToken);
        logger.LogInformation(
            "[GAME SESSION] Sessão {SessionId} criada para usuário {UserId} e personagem {CharacterId}",
            session.Id,
            userId,
            characterId);

        return new(
            token,
            expiresAt,
            characterId,
            _serverOptions.DefaultHost,
            _serverOptions.DefaultPort);
    }

    public async Task<ValidateGameSessionResponse> ValidateAndConsumeAsync(
        string sessionToken,
        string gameServerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken)
            || sessionToken.Length > 512
            || string.IsNullOrWhiteSpace(gameServerId)
            || gameServerId.Length > 64)
        {
            return Invalid("INVALID_REQUEST");
        }

        ConsumeGameSessionResult result = await gameSessionRepository.TryConsumeAsync(
            tokenProtector.ComputeHash(sessionToken),
            gameServerId.Trim(),
            timeProvider.GetUtcNow(),
            cancellationToken);

        if (result.Status != ConsumeGameSessionStatus.Consumed || result.Session is null)
        {
            string errorCode = result.Status switch
            {
                ConsumeGameSessionStatus.Expired => "SESSION_EXPIRED",
                ConsumeGameSessionStatus.AlreadyConsumed => "SESSION_ALREADY_CONSUMED",
                ConsumeGameSessionStatus.CharacterNotFound => "CHARACTER_NOT_FOUND",
                ConsumeGameSessionStatus.UserNotFound => "USER_NOT_FOUND",
                _ => "SESSION_NOT_FOUND",
            };

            logger.LogWarning("[GAME SESSION] Validação rejeitada: {ErrorCode}", errorCode);
            return Invalid(errorCode);
        }

        GameSession session = result.Session;
        if (session.User.IsBlocked)
        {
            logger.LogWarning(
                "[GAME SESSION] Usuário bloqueado rejeitado para sessão {SessionId}",
                session.Id);
            return Invalid("USER_NOT_FOUND");
        }

        Character character = session.Character;
        logger.LogInformation(
            "[GAME SESSION] Sessão {SessionId} consumida por {GameServerId}",
            session.Id,
            gameServerId);

        return new(
            Valid: true,
            UserId: session.UserId,
            CharacterId: session.CharacterId,
            Username: session.User.Username,
            CharacterName: character.Name,
            Level: character.Level,
            Experience: character.Experience,
            CurrentHealth: character.CurrentHealth,
            MaxHealth: character.MaxHealth,
            MapId: character.MapId,
            PositionX: character.PositionX,
            PositionY: character.PositionY);
    }

    private static ValidateGameSessionResponse Invalid(string errorCode) =>
        new(false, errorCode);
}
