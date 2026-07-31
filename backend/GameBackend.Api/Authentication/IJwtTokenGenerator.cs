using GameBackend.Api.Entities;

namespace GameBackend.Api.Authentication;

public interface IJwtTokenGenerator
{
    GeneratedToken Generate(User user);
}

public sealed record GeneratedToken(string AccessToken, DateTimeOffset Expiration);
