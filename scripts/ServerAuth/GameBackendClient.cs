using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class GameBackendClient : IGameBackendClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly string _internalApiKey;
    private readonly string _gameServerId;

    public GameBackendClient(
        string baseUrl,
        string internalApiKey,
        string gameServerId,
        int timeoutSeconds)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri))
            throw new ArgumentException("GAME_API_URL inválida.", nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(internalApiKey))
            throw new ArgumentException("GAME_SERVER_API_KEY não configurada.", nameof(internalApiKey));
        if (string.IsNullOrWhiteSpace(gameServerId))
            throw new ArgumentException("GAME_SERVER_ID não configurado.", nameof(gameServerId));

        _internalApiKey = internalApiKey;
        _gameServerId = gameServerId;
        _httpClient = new HttpClient
        {
            BaseAddress = uri,
            Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 60)),
        };
    }

    public async Task<GameSessionValidationResult> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateInternalRequest(
            HttpMethod.Post,
            "api/internal/game-sessions/validate");
        request.Content = JsonContent.Create(
            new ValidateGameSessionApiRequest(sessionToken, _gameServerId),
            options: JsonOptions);

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        ValidateGameSessionApiResponse? payload = await response.Content
            .ReadFromJsonAsync<ValidateGameSessionApiResponse>(
                JsonOptions,
                cancellationToken);

        if (!response.IsSuccessStatusCode || payload is null || !payload.Valid)
            return new(false, payload?.ErrorCode ?? "AUTH_SERVICE_UNAVAILABLE");

        if (payload.UserId is null
            || payload.CharacterId is null
            || payload.CurrentHealth is null
            || payload.MaxHealth is null
            || payload.Level is null
            || payload.Experience is null
            || payload.PositionX is null
            || payload.PositionY is null
            || string.IsNullOrWhiteSpace(payload.Username)
            || string.IsNullOrWhiteSpace(payload.CharacterName)
            || string.IsNullOrWhiteSpace(payload.MapId))
        {
            return new(false, "INVALID_API_RESPONSE");
        }

        AuthenticatedCharacterData character = new()
        {
            UserId = payload.UserId.Value,
            CharacterId = payload.CharacterId.Value,
            Username = payload.Username,
            CharacterName = payload.CharacterName,
            Level = payload.Level.Value,
            Experience = payload.Experience.Value,
            CurrentHealth = payload.CurrentHealth.Value,
            MaxHealth = payload.MaxHealth.Value,
            MapId = payload.MapId,
            PositionX = payload.PositionX.Value,
            PositionY = payload.PositionY.Value,
        };
        return new(true, Character: character);
    }

    public async Task<bool> SaveCharacterStateAsync(
        AuthenticatedPlayerState state,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateInternalRequest(
            HttpMethod.Put,
            $"api/internal/characters/{state.CharacterId:D}/state");
        request.Content = JsonContent.Create(
            new SaveCharacterStateApiRequest(
                state.UserId,
                state.CurrentHealth,
                state.MapId,
                state.PositionX,
                state.PositionY),
            options: JsonOptions);

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private HttpRequestMessage CreateInternalRequest(HttpMethod method, string path)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Add("X-Game-Server-Key", _internalApiKey);
        return request;
    }

    public void Dispose() => _httpClient.Dispose();
}
