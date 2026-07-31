namespace GameBackend.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string email,
        string username,
        string resetToken,
        CancellationToken cancellationToken);
}
