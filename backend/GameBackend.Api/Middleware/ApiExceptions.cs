namespace GameBackend.Api.Middleware;

public sealed class ApiValidationException(string message, string? errorCode = null)
    : Exception(message)
{
    public string? ErrorCode { get; } = errorCode;
}

public sealed class NotFoundApiException(string message) : Exception(message);

public sealed class UnauthorizedApiException(string message) : Exception(message);
