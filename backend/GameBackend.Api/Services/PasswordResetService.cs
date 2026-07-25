using GameBackend.Api.Authentication;
using GameBackend.Api.Configuration;
using GameBackend.Api.Data;
using GameBackend.Api.DTOs.Authentication;
using GameBackend.Api.Entities;
using GameBackend.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameBackend.Api.Services;

public sealed class PasswordResetService(
    GameDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    PasswordResetTokenProtector tokenProtector,
    IOptions<PasswordResetOptions> options,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private const string GenericMessage =
        "Se existir uma conta com esse email, você receberá as instruções.";

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        User? user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);
        if (user is null || user.IsBlocked)
            return new ForgotPasswordResponse(GenericMessage);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await dbContext.PasswordResetTokens
            .Where(token => token.UserId == user.Id && !token.IsConsumed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.IsConsumed, true)
                    .SetProperty(token => token.ConsumedAt, now),
                cancellationToken);

        string rawToken = tokenProtector.GenerateToken();
        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenProtector.ComputeHash(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(options.Value.ExpirationMinutes),
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordResetAsync(
            user.Email,
            user.Username,
            rawToken,
            cancellationToken);
        logger.LogInformation(
            "Recuperação de senha solicitada para o usuário {UserId}",
            user.Id);

        string? developmentToken =
            environment.IsDevelopment() && options.Value.ExposeTokenInDevelopment
                ? rawToken
                : null;
        return new ForgotPasswordResponse(GenericMessage, developmentToken);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        string tokenHash = tokenProtector.ComputeHash(request.Token.Trim());
        DateTimeOffset now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        User? user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);
        if (user is null || user.IsBlocked)
            throw InvalidToken();

        PasswordResetToken? token = await dbContext.PasswordResetTokens
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == user.Id
                    && candidate.TokenHash == tokenHash,
                cancellationToken);
        if (token is null || token.IsConsumed)
            throw InvalidToken();
        if (token.ExpiresAt <= now)
            throw new ApiValidationException(
                "Este link de recuperação expirou. Solicite um novo.",
                "RESET_TOKEN_EXPIRED");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        token.IsConsumed = true;
        token.ConsumedAt = now;
        await dbContext.PasswordResetTokens
            .Where(candidate => candidate.UserId == user.Id
                && candidate.Id != token.Id
                && !candidate.IsConsumed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.IsConsumed, true)
                    .SetProperty(candidate => candidate.ConsumedAt, now),
                cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Senha redefinida para o usuário {UserId}", user.Id);
        return new ResetPasswordResponse("Senha redefinida com sucesso.");
    }

    private static ApiValidationException InvalidToken() =>
        new("Não foi possível redefinir a senha.", "RESET_TOKEN_INVALID");

}
