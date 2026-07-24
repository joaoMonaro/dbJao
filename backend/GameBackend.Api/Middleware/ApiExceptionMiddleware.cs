using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger,
    IHostEnvironment environment
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception
    )
    {
        (int statusCode, string title) = exception switch
        {
            ApiValidationException => (
                StatusCodes.Status400BadRequest,
                "Requisição inválida"
            ),
            UnauthorizedApiException => (
                StatusCodes.Status401Unauthorized,
                "Não autorizado"
            ),
            NotFoundApiException => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno do servidor"
            ),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Erro não tratado em {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );
        }
        else
        {
            logger.LogWarning(
                "Requisição rejeitada em {Method} {Path}: {Reason}",
                context.Request.Method,
                context.Request.Path,
                exception.Message
            );
        }

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= StatusCodes.Status500InternalServerError
                && !environment.IsDevelopment()
                    ? "Ocorreu um erro inesperado."
                    : exception.Message,
            Instance = context.Request.Path,
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken: context.RequestAborted
        );
    }
}
