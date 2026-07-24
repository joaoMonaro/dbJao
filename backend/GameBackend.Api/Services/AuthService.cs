using GameBackend.Api.Authentication;
using GameBackend.Api.DTOs.Authentication;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using GameBackend.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameBackend.Api.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<AuthService> logger
) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        string username = request.Username.Trim();
        string email = request.Email.Trim();

        if (string.IsNullOrWhiteSpace(username))
            throw new ApiValidationException("Username é obrigatório.");

        if (await userRepository.UsernameExistsAsync(username, cancellationToken))
            throw new ApiValidationException("Username já está em uso.");

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
            throw new ApiValidationException("E-mail já está em uso.");

        User user = new()
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
        };

        try
        {
            await userRepository.AddAsync(user, cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ApiValidationException("Username ou e-mail já está em uso.");
        }

        logger.LogInformation(
            "Cadastro realizado para o usuário {UserId} ({Username})",
            user.Id,
            user.Username
        );

        return new RegisterResponse(user.Id, user.Username, user.Email, user.CreatedAt);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        string email = request.Email.Trim();
        User? user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Tentativa de login inválida para o e-mail {Email}", email);
            throw new UnauthorizedApiException("E-mail ou senha inválidos.");
        }

        GeneratedToken token = jwtTokenGenerator.Generate(user);

        logger.LogInformation(
            "Login realizado para o usuário {UserId} ({Username})",
            user.Id,
            user.Username
        );

        return new AuthResponse(token.AccessToken, token.Expiration, user.Username);
    }
}
