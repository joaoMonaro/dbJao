using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GameBackend.Api.Authentication;

public sealed class GameSessionTokenProtector : IGameSessionTokenProtector
{
    private const int TokenSizeBytes = 32;

    public string GenerateToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSizeBytes));
    }

    public string ComputeHash(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(hash);
    }
}
