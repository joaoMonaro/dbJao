using GameBackend.Api.Authentication;
using Xunit;

namespace GameBackend.Api.Tests;

public sealed class GameSessionSecurityTests
{
    [Fact]
    public void GeneratedTokensAreRandomAndOnlyFixedLengthHashesAreStored()
    {
        GameSessionTokenProtector protector = new();

        string firstToken = protector.GenerateToken();
        string secondToken = protector.GenerateToken();
        string firstHash = protector.ComputeHash(firstToken);

        Assert.NotEqual(firstToken, secondToken);
        Assert.NotEqual(firstToken, firstHash);
        Assert.Equal(64, firstHash.Length);
    }
}
