using System.Security.Cryptography;
using System.Text;
using GameBackend.Api.Configuration;
using Microsoft.Extensions.Options;

namespace GameBackend.Api.Authentication;

public sealed class GameServerKeyValidator(IOptions<GameServerOptions> options)
    : IGameServerKeyValidator
{
    private readonly byte[] _expectedHash = SHA256.HashData(
        Encoding.UTF8.GetBytes(options.Value.InternalApiKey)
    );

    public bool IsValid(string? providedKey)
    {
        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        byte[] providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));
        return CryptographicOperations.FixedTimeEquals(_expectedHash, providedHash);
    }
}
