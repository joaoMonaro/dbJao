using System.Security.Cryptography;
using System.Text;

namespace GameBackend.Api.Authentication;

public sealed class PasswordResetTokenProtector
{
    public string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
