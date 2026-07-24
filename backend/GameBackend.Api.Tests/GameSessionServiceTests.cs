using Xunit;
using System.Collections.Concurrent;
using GameBackend.Api.Authentication;
using GameBackend.Api.Configuration;
using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using GameBackend.Api.Repositories;
using GameBackend.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameBackend.Api.Tests;

public sealed class GameSessionServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CharacterId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UserCreatesSessionForOwnCharacter()
    {
        TestContext context = CreateContext();

        CreateGameSessionResponse response = await context.Service.CreateSessionAsync(
            UserId,
            CharacterId,
            CancellationToken.None);

        Assert.NotEmpty(response.SessionToken);
        Assert.Equal(CharacterId, response.CharacterId);
        Assert.Equal(Now.AddSeconds(60), response.ExpiresAt);
        Assert.DoesNotContain(response.SessionToken, context.Sessions.StoredTokenHashes);
    }

    [Fact]
    public async Task UserCannotCreateSessionForAnotherUsersCharacter()
    {
        TestContext context = CreateContext(characterBelongsToUser: false);

        await Assert.ThrowsAsync<NotFoundApiException>(() =>
            context.Service.CreateSessionAsync(
                UserId,
                CharacterId,
                CancellationToken.None));
    }

    [Fact]
    public async Task MissingCharacterIsRejected()
    {
        TestContext context = CreateContext(hasCharacter: false);

        await Assert.ThrowsAsync<NotFoundApiException>(() =>
            context.Service.CreateSessionAsync(
                UserId,
                CharacterId,
                CancellationToken.None));
    }

    [Fact]
    public async Task ValidTokenReturnsTrustedCharacterAndIsConsumed()
    {
        TestContext context = CreateContext();
        CreateGameSessionResponse created = await context.Service.CreateSessionAsync(
            UserId,
            CharacterId,
            CancellationToken.None);

        ValidateGameSessionResponse result = await context.Service.ValidateAndConsumeAsync(
            created.SessionToken,
            "test-server",
            CancellationToken.None);

        Assert.True(result.Valid);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(CharacterId, result.CharacterId);
        Assert.Equal("Goku", result.CharacterName);
        Assert.Equal(100, result.MaxHealth);
    }

    [Fact]
    public async Task ExpiredTokenIsRejected()
    {
        TestContext context = CreateContext();
        CreateGameSessionResponse created = await context.Service.CreateSessionAsync(
            UserId,
            CharacterId,
            CancellationToken.None);
        context.Time.Advance(TimeSpan.FromSeconds(61));

        ValidateGameSessionResponse result = await context.Service.ValidateAndConsumeAsync(
            created.SessionToken,
            "test-server",
            CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Equal("SESSION_EXPIRED", result.ErrorCode);
    }

    [Fact]
    public async Task ConsumedTokenIsRejectedOnReuse()
    {
        TestContext context = CreateContext();
        CreateGameSessionResponse created = await context.Service.CreateSessionAsync(
            UserId,
            CharacterId,
            CancellationToken.None);

        ValidateGameSessionResponse first = await context.Service.ValidateAndConsumeAsync(
            created.SessionToken,
            "server-a",
            CancellationToken.None);
        ValidateGameSessionResponse second = await context.Service.ValidateAndConsumeAsync(
            created.SessionToken,
            "server-b",
            CancellationToken.None);

        Assert.True(first.Valid);
        Assert.False(second.Valid);
        Assert.Equal("SESSION_ALREADY_CONSUMED", second.ErrorCode);
    }

    [Fact]
    public async Task TwoConcurrentAttemptsConsumeExactlyOnce()
    {
        TestContext context = CreateContext();
        CreateGameSessionResponse created = await context.Service.CreateSessionAsync(
            UserId,
            CharacterId,
            CancellationToken.None);

        Task<ValidateGameSessionResponse>[] attempts =
        [
            context.Service.ValidateAndConsumeAsync(
                created.SessionToken,
                "server-a",
                CancellationToken.None),
            context.Service.ValidateAndConsumeAsync(
                created.SessionToken,
                "server-b",
                CancellationToken.None),
        ];

        ValidateGameSessionResponse[] results = await Task.WhenAll(attempts);
        Assert.Single(results, result => result.Valid);
        Assert.Single(results, result => result.ErrorCode == "SESSION_ALREADY_CONSUMED");
    }

    [Fact]
    public void InvalidInternalApiKeyIsRejected()
    {
        GameServerKeyValidator validator = new(Options.Create(new GameServerOptions
        {
            InternalApiKey = "correct-internal-key-with-at-least-32-bytes",
        }));

        Assert.True(validator.IsValid("correct-internal-key-with-at-least-32-bytes"));
        Assert.False(validator.IsValid("wrong-key"));
        Assert.False(validator.IsValid(null));
    }

    [Fact]
    public async Task MissingUserIsRejected()
    {
        TestContext context = CreateContext(hasUser: false);

        await Assert.ThrowsAsync<UnauthorizedApiException>(() =>
            context.Service.CreateSessionAsync(
                UserId,
                CharacterId,
                CancellationToken.None));
    }

    private static TestContext CreateContext(
        bool hasUser = true,
        bool hasCharacter = true,
        bool characterBelongsToUser = true)
    {
        User? user = hasUser
            ? new User { Id = UserId, Username = "account", Email = "account@test.local" }
            : null;
        Character? character = hasCharacter
            ? new Character
            {
                Id = CharacterId,
                UserId = characterBelongsToUser ? UserId : Guid.NewGuid(),
                Name = "Goku",
                CurrentHealth = 75,
                MaxHealth = 100,
                MapId = "kame_house",
                PositionX = 100,
                PositionY = 200,
                User = user!,
            }
            : null;

        FakeUserRepository users = new(user);
        FakeCharacterRepository characters = new(character);
        FakeGameSessionRepository sessions = new(() => Now);
        MutableTimeProvider time = new(Now);
        GameSessionTokenProtector protector = new();
        GameSessionService service = new(
            users,
            characters,
            sessions,
            protector,
            Options.Create(new GameSessionOptions
            {
                ExpirationSeconds = 60,
                MaxPendingSessionsPerUser = 3,
            }),
            Options.Create(new GameServerOptions
            {
                InternalApiKey = "test-internal-key-with-at-least-32-bytes",
                DefaultHost = "127.0.0.1",
                DefaultPort = 7000,
            }),
            time,
            NullLogger<GameSessionService>.Instance);

        sessions.UtcNow = () => time.GetUtcNow();
        return new(service, sessions, time);
    }

    private sealed record TestContext(
        GameSessionService Service,
        FakeGameSessionRepository Sessions,
        MutableTimeProvider Time);

    private sealed class MutableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        private DateTimeOffset _current = current;
        public override DateTimeOffset GetUtcNow() => _current;
        public void Advance(TimeSpan value) => _current += value;
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Id == id ? user : null);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Email == email ? user : null);
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Email == email);
        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Username == username);
        public Task AddAsync(User entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeCharacterRepository(Character? character) : ICharacterRepository
    {
        public Task<Character?> GetByIdForUserAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                character?.Id == id && character.UserId == userId ? character : null);
        public Task<IReadOnlyList<Character>> GetAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Character>>([]);
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(0);
        public Task<bool> NameExistsForUserAsync(
            Guid userId,
            string name,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task AddAsync(Character entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<bool> UpdateStateAsync(
            Guid characterId,
            Guid userId,
            int currentHealth,
            string mapId,
            float positionX,
            float positionY,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeGameSessionRepository(Func<DateTimeOffset> initialNow)
        : IGameSessionRepository
    {
        private readonly object _gate = new();
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
        public Func<DateTimeOffset> UtcNow { get; set; } = initialNow;
        public IEnumerable<string> StoredTokenHashes => _sessions.Keys;

        public Task<int> CountPendingByUserAsync(
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(_sessions.Values.Count(session =>
                session.UserId == userId && !session.IsConsumed && session.ExpiresAt > now));

        public Task<bool> HasPendingForCharacterAsync(
            Guid userId,
            Guid characterId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(_sessions.Values.Any(session =>
                session.UserId == userId
                && session.CharacterId == characterId
                && !session.IsConsumed
                && session.ExpiresAt > now));

        public Task AddAsync(GameSession session, CancellationToken cancellationToken)
        {
            _sessions[session.TokenHash] = session;
            return Task.CompletedTask;
        }

        public Task<ConsumeGameSessionResult> TryConsumeAsync(
            string tokenHash,
            string gameServerId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_sessions.TryGetValue(tokenHash, out GameSession? session))
                    return Task.FromResult(new ConsumeGameSessionResult(
                        ConsumeGameSessionStatus.NotFound));
                if (session.IsConsumed)
                    return Task.FromResult(new ConsumeGameSessionResult(
                        ConsumeGameSessionStatus.AlreadyConsumed));
                if (session.ExpiresAt <= UtcNow())
                    return Task.FromResult(new ConsumeGameSessionResult(
                        ConsumeGameSessionStatus.Expired));

                session.IsConsumed = true;
                session.ConsumedAt = now;
                session.GameServerId = gameServerId;
                session.User = new User
                {
                    Id = UserId,
                    Username = "account",
                    Email = "account@test.local",
                };
                session.Character = new Character
                {
                    Id = CharacterId,
                    UserId = UserId,
                    Name = "Goku",
                    CurrentHealth = 75,
                    MaxHealth = 100,
                    MapId = "kame_house",
                    PositionX = 100,
                    PositionY = 200,
                };
                return Task.FromResult(new ConsumeGameSessionResult(
                    ConsumeGameSessionStatus.Consumed,
                    session));
            }
        }
    }
}
