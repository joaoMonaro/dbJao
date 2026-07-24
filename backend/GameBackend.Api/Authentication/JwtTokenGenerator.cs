using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameBackend.Api.Configuration;
using GameBackend.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameBackend.Api.Authentication;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public GeneratedToken Generate(User user)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiration = now.AddMinutes(_jwtOptions.ExpirationMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        ];

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(_jwtOptions.Secret)
        );
        SigningCredentials credentials = new(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        JwtSecurityToken token = new(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiration.UtcDateTime,
            signingCredentials: credentials
        );

        return new GeneratedToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiration
        );
    }
}
