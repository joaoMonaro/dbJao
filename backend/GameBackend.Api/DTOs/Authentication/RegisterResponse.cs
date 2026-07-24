namespace GameBackend.Api.DTOs.Authentication;

public sealed record RegisterResponse(
    Guid Id,
    string Username,
    string Email,
    DateTimeOffset CreatedAt
);
