using GameBackend.Api.Authentication;
using Xunit;

namespace GameBackend.Api.Tests;

public sealed class PasswordResetSecurityTests
{
    [Fact]
    public void TokensAreRandomAndOnlyHashesHaveFixedStorageLength()
    {
        PasswordResetTokenProtector protector = new();
        string first = protector.GenerateToken();
        string second = protector.GenerateToken();
        string hash = protector.ComputeHash(first);

        Assert.NotEqual(first, second);
        Assert.DoesNotContain(first, hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void SameTokenProducesStableHashForLookup()
    {
        PasswordResetTokenProtector protector = new();
        string token = protector.GenerateToken();
        Assert.Equal(protector.ComputeHash(token), protector.ComputeHash(token));
    }
}
