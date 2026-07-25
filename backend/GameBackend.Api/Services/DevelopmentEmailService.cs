using System.Text;
using GameBackend.Api.Configuration;
using Microsoft.Extensions.Options;

namespace GameBackend.Api.Services;

public sealed class DevelopmentEmailService(
    IOptions<PasswordResetOptions> options,
    ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public async Task SendPasswordResetAsync(
        string email,
        string username,
        string resetToken,
        CancellationToken cancellationToken)
    {
        string directory = options.Value.DevelopmentOutputDirectory;
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, $"{Guid.CreateVersion7():N}.txt");
        string content =
            $"Destinatário: {email}{Environment.NewLine}"
            + $"Olá, {username}.{Environment.NewLine}"
            + $"Token de redefinição: {resetToken}{Environment.NewLine}";
        await File.WriteAllTextAsync(file, content, Encoding.UTF8, cancellationToken);
        logger.LogInformation(
            "Mensagem de recuperação gerada no arquivo de desenvolvimento {FileName}",
            Path.GetFileName(file));
    }
}
