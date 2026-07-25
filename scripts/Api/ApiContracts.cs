using System;
using System.Collections.Generic;

public sealed record LoginApiRequest(string Email, string Password);
public sealed record LoginApiResponse(string AccessToken, DateTimeOffset Expiration, string Username);
public sealed record RegisterApiRequest(string Username, string Email, string Password);
public sealed record RegisterApiResponse(
    Guid Id,
    string Username,
    string Email,
    DateTimeOffset CreatedAt);
public sealed record ForgotPasswordApiRequest(string Email);
public sealed record ForgotPasswordApiResponse(string Message, string? DevelopmentToken);
public sealed record ResetPasswordApiRequest(string Email, string Token, string NewPassword);
public sealed record ResetPasswordApiResponse(string Message);

public sealed record CharacterApiResponse(
    Guid Id,
    string Name,
    int Level,
    long Experience,
    int CurrentHealth,
    string MapId,
    float PositionX,
    float PositionY);

public sealed record CreateCharacterApiRequest(string Name);
public sealed record CreateGameSessionApiRequest(Guid CharacterId);

public sealed record CreateGameSessionApiResponse(
    string SessionToken,
    DateTimeOffset ExpiresAt,
    Guid CharacterId,
    string GameServerHost,
    int GameServerPort);

public sealed record ValidateGameSessionApiRequest(string SessionToken, string GameServerId);

public sealed class ValidateGameSessionApiResponse
{
    public bool Valid { get; init; }
    public string? ErrorCode { get; init; }
    public Guid? UserId { get; init; }
    public Guid? CharacterId { get; init; }
    public string? Username { get; init; }
    public string? CharacterName { get; init; }
    public int? Level { get; init; }
    public long? Experience { get; init; }
    public int? CurrentHealth { get; init; }
    public int? MaxHealth { get; init; }
    public string? MapId { get; init; }
    public float? PositionX { get; init; }
    public float? PositionY { get; init; }
}

public sealed record SaveCharacterStateApiRequest(
    Guid UserId,
    int CurrentHealth,
    string MapId,
    float PositionX,
    float PositionY);

public sealed class ApiRequestException(
    string message,
    int statusCode = 0,
    string? errorCode = null,
    TimeSpan? retryAfter = null)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? ErrorCode { get; } = errorCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
