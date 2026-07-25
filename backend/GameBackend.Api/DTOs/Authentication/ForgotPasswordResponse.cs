namespace GameBackend.Api.DTOs.Authentication;

public sealed record ForgotPasswordResponse(string Message, string? DevelopmentToken = null);
