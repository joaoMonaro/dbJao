namespace GameBackend.Api.DTOs.Authentication;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset Expiration,
    string Username
);
