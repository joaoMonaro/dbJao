namespace GameBackend.Api.Middleware;

public sealed class ApiValidationException(string message) : Exception(message);

public sealed class NotFoundApiException(string message) : Exception(message);

public sealed class UnauthorizedApiException(string message) : Exception(message);
