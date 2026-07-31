using GameBackend.Api.Data;
using GameBackend.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Api.Extensions;

public static class ApplicationExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(
        this IApplicationBuilder application
    )
    {
        return application.UseMiddleware<ApiExceptionMiddleware>();
    }

    public static async Task ApplyDatabaseMigrationsAsync(
        this WebApplication application
    )
    {
        if (!application.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
            return;

        const int maximumAttempts = 5;
        ILogger logger = application.Logger;

        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                await using AsyncServiceScope scope =
                    application.Services.CreateAsyncScope();
                GameDbContext dbContext =
                    scope.ServiceProvider.GetRequiredService<GameDbContext>();

                logger.LogInformation(
                    "Aplicando migrations do banco de dados (tentativa {Attempt}/{MaximumAttempts})",
                    attempt,
                    maximumAttempts
                );

                await dbContext.Database.MigrateAsync(
                    application.Lifetime.ApplicationStopping
                );

                logger.LogInformation("Migrations aplicadas com sucesso");
                return;
            }
            catch (Exception exception) when (attempt < maximumAttempts)
            {
                TimeSpan delay = TimeSpan.FromSeconds(attempt * 2);
                logger.LogWarning(
                    exception,
                    "Falha ao aplicar migrations. Nova tentativa em {DelaySeconds} segundos",
                    delay.TotalSeconds
                );
                await Task.Delay(delay, application.Lifetime.ApplicationStopping);
            }
        }

        await using AsyncServiceScope finalScope =
            application.Services.CreateAsyncScope();
        GameDbContext finalDbContext =
            finalScope.ServiceProvider.GetRequiredService<GameDbContext>();
        await finalDbContext.Database.MigrateAsync(
            application.Lifetime.ApplicationStopping
        );
    }
}
