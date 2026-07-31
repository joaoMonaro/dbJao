using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ApiClient(string baseUrl, int timeoutSeconds = 10)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
            throw new ArgumentException("GAME_API_URL inválida.", nameof(baseUrl));

        _httpClient = new HttpClient
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 60)),
        };
    }

    public Task<LoginApiResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default) =>
        SendAsync<LoginApiResponse>(
            HttpMethod.Post,
            "api/auth/login",
            new LoginApiRequest(email, password),
            null,
            cancellationToken);

    public Task<RegisterApiResponse> RegisterAsync(
        RegisterApiRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<RegisterApiResponse>(
            HttpMethod.Post, "api/auth/register", request, null, cancellationToken);

    public Task<ForgotPasswordApiResponse> ForgotPasswordAsync(
        ForgotPasswordApiRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<ForgotPasswordApiResponse>(
            HttpMethod.Post, "api/auth/forgot-password", request, null, cancellationToken);

    public Task<ResetPasswordApiResponse> ResetPasswordAsync(
        ResetPasswordApiRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<ResetPasswordApiResponse>(
            HttpMethod.Post, "api/auth/reset-password", request, null, cancellationToken);

    public Task<IReadOnlyList<CharacterApiResponse>> GetCharactersAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<CharacterApiResponse>>(
            HttpMethod.Get,
            "api/characters",
            null,
            accessToken,
            cancellationToken);

    public Task<CharacterApiResponse> CreateCharacterAsync(
        string accessToken,
        string name,
        CancellationToken cancellationToken = default) =>
        SendAsync<CharacterApiResponse>(
            HttpMethod.Post,
            "api/characters",
            new CreateCharacterApiRequest(name),
            accessToken,
            cancellationToken);

    public Task<CreateGameSessionApiResponse> CreateGameSessionAsync(
        string accessToken,
        Guid characterId,
        CancellationToken cancellationToken = default) =>
        SendAsync<CreateGameSessionApiResponse>(
            HttpMethod.Post,
            "api/game-sessions",
            new CreateGameSessionApiRequest(characterId),
            accessToken,
            cancellationToken);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            (string message, string? errorCode) =
                await ReadErrorAsync(response, cancellationToken);
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            throw new ApiRequestException(
                message, (int)response.StatusCode, errorCode, retryAfter);
        }

        T? result = await response.Content.ReadFromJsonAsync<T>(
            JsonOptions,
            cancellationToken);
        return result ?? throw new ApiRequestException("A API retornou uma resposta vazia.");
    }

    private static async Task<(string Message, string? ErrorCode)> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            string? errorCode = document.RootElement.TryGetProperty(
                "errorCode", out JsonElement code) ? code.GetString() : null;
            if (document.RootElement.TryGetProperty("detail", out JsonElement detail))
                return (detail.GetString() ?? "Falha na requisição.", errorCode);
            if (document.RootElement.TryGetProperty("title", out JsonElement title))
                return (title.GetString() ?? "Falha na requisição.", errorCode);
        }
        catch (JsonException)
        {
        }

        return ("Falha na requisição.", null);
    }

    public void Dispose() => _httpClient.Dispose();
}
