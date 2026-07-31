using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class AuthManager(ApiClient apiClient)
{
    public string? AccessToken { get; private set; }
    public string? Username { get; private set; }
    public DateTimeOffset Expiration { get; private set; }
    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken) && Expiration > DateTimeOffset.UtcNow;

    public async Task LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        LoginApiResponse response = await apiClient.LoginAsync(
            email,
            password,
            cancellationToken);
        AccessToken = response.AccessToken;
        Username = response.Username;
        Expiration = response.Expiration;
    }

    public string GetRequiredAccessToken()
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Faça login novamente.");
        return AccessToken!;
    }

    public void Logout()
    {
        AccessToken = null;
        Username = null;
        Expiration = default;
    }
}
