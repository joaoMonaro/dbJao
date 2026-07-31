using GameBackend.Api.Authentication;
using GameBackend.Api.DTOs.Authentication;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using GameBackend.Api.Repositories;
using GameBackend.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameBackend.Api.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterNormalizesEmailAndHashesPassword()
    {
        FakeUsers users = new();
        FakePasswordHasher passwords = new();
        AuthService service = Create(users, passwords);

        RegisterResponse response = await service.RegisterAsync(
            new RegisterRequest
            {
                Username = "Jogador",
                Email = "  USER@Example.COM ",
                Password = "secret123",
            },
            default);

        Assert.Equal("user@example.com", response.Email);
        Assert.Equal("hash:secret123", users.Added!.PasswordHash);
    }

    [Fact]
    public async Task RegisterRejectsDuplicateEmail()
    {
        FakeUsers users = new() { EmailExists = true };
        await Assert.ThrowsAsync<ApiValidationException>(() =>
            Create(users, new FakePasswordHasher()).RegisterAsync(
                new RegisterRequest
                {
                    Username = "Jogador",
                    Email = "user@example.com",
                    Password = "secret123",
                },
                default));
    }

    [Fact]
    public async Task LoginReturnsTokenWithoutKeepingPassword()
    {
        FakeUsers users = new()
        {
            Found = new User
            {
                Username = "Jogador",
                Email = "user@example.com",
                PasswordHash = "hash:secret123",
            },
        };
        AuthResponse response = await Create(users, new FakePasswordHasher()).LoginAsync(
            new LoginRequest { Email = "USER@example.com", Password = "secret123" },
            default);
        Assert.Equal("jwt", response.AccessToken);
    }

    [Fact]
    public async Task LoginRejectsInvalidPassword()
    {
        FakeUsers users = new()
        {
            Found = new User
            {
                Email = "user@example.com",
                PasswordHash = "hash:correct-password",
            },
        };
        await Assert.ThrowsAsync<UnauthorizedApiException>(() =>
            Create(users, new FakePasswordHasher()).LoginAsync(
                new LoginRequest
                {
                    Email = "user@example.com",
                    Password = "wrong-password",
                },
                default));
    }

    private static AuthService Create(FakeUsers users, FakePasswordHasher passwords) =>
        new(users, passwords, new FakeJwt(), NullLogger<AuthService>.Instance);

    private sealed class FakeUsers : IUserRepository
    {
        public bool EmailExists { get; init; }
        public bool UsernameExists { get; init; }
        public User? Found { get; init; }
        public User? Added { get; private set; }
        public Task<User?> GetByIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(Found);
        public Task<User?> GetByEmailAsync(string email, CancellationToken token) =>
            Task.FromResult(Found?.Email == email ? Found : null);
        public Task<bool> EmailExistsAsync(string email, CancellationToken token) =>
            Task.FromResult(EmailExists);
        public Task<bool> UsernameExistsAsync(string username, CancellationToken token) =>
            Task.FromResult(UsernameExists);
        public Task AddAsync(User user, CancellationToken token)
        {
            Added = user;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => hash == $"hash:{password}";
    }

    private sealed class FakeJwt : IJwtTokenGenerator
    {
        public GeneratedToken Generate(User user) =>
            new("jwt", DateTimeOffset.UtcNow.AddHours(1));
    }
}
