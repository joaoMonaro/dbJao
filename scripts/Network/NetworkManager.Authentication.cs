using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public partial class NetworkManager
{
    private const int MaximumSessionTokenLength = 512;
    private const float AuthenticationTimeoutCheckInterval = 0.25f;

    private readonly Dictionary<int, PendingConnection> _pendingConnections = new();
    private readonly Dictionary<int, AuthenticatedCharacterData> _authenticatedPeers = new();
    private readonly Dictionary<Guid, int> _onlineCharacters = new();
    private readonly List<int> _expiredPeers = [];

    private GameBackendClient? _gameBackendClient;
    private ICharacterPersistenceService? _characterPersistenceService;
    private CancellationTokenSource? _serverLifetime;
    private double _authenticationTimeoutSeconds = 10.0;
    private float _authenticationTimeoutCheckElapsed;

    private ApiClient? _apiClient;
    private AuthManager? _authManager;
    private CharacterSelectionManager? _characterSelection;
    private GameConnectionManager? _gameConnection;
    private string? _pendingSessionToken;
    private DateTimeOffset _pendingSessionExpiration;
    private PanelContainer? _authenticationPanel;
    private LineEdit? _emailInput;
    private LineEdit? _passwordInput;
    private LineEdit? _characterNameInput;
    private Button? _loginButton;
    private Button? _refreshCharactersButton;
    private Button? _createCharacterButton;
    private Button? _enterGameButton;
    private OptionButton? _characterOptions;
    private AuthRoot? _authRoot;

    public override void _Process(double delta)
    {
        if (!RunningAsServer || _pendingConnections.Count == 0)
            return;

        _authenticationTimeoutCheckElapsed += (float)delta;
        if (_authenticationTimeoutCheckElapsed < AuthenticationTimeoutCheckInterval)
            return;

        _authenticationTimeoutCheckElapsed = 0.0f;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _expiredPeers.Clear();
        foreach ((int peerId, PendingConnection pending) in _pendingConnections)
        {
            if (pending.State == ConnectionAuthState.AwaitingAuthentication
                && pending.AuthenticationDeadline <= now)
            {
                _expiredPeers.Add(peerId);
            }
        }

        foreach (int peerId in _expiredPeers)
        {
            GD.Print($"[AUTH] Timeout para peer {peerId}");
            RejectPeer(peerId, "AUTH_TIMEOUT");
        }
    }

    private bool InitializeServerAuthentication()
    {
        string apiUrl = GetEnvironmentOrDefault("GAME_API_URL", "http://127.0.0.1:5000");
        string apiKey = System.Environment.GetEnvironmentVariable("GAME_SERVER_API_KEY")?.Trim()
            ?? string.Empty;
        string serverId = GetEnvironmentOrDefault("GAME_SERVER_ID", "local-server-01");
        int requestTimeout = GetPositiveEnvironmentInt(
            "GAME_API_TIMEOUT_SECONDS",
            5,
            1,
            60);
        _authenticationTimeoutSeconds = GetPositiveEnvironmentInt(
            "GAME_AUTH_TIMEOUT_SECONDS",
            10,
            3,
            60);

        try
        {
            _gameBackendClient = new GameBackendClient(
                apiUrl,
                apiKey,
                serverId,
                requestTimeout);
            _characterPersistenceService = new CharacterPersistenceService(_gameBackendClient);
            _serverLifetime = new CancellationTokenSource();
            GD.Print($"[AUTH] Backend configurado em {apiUrl}; servidor {serverId}");
            return true;
        }
        catch (ArgumentException exception)
        {
            GD.PushError($"[AUTH] Configuração inválida: {exception.Message}");
            return false;
        }
    }

    private void RegisterPendingPeer(int peerId)
    {
        if (_pendingConnections.ContainsKey(peerId) || _authenticatedPeers.ContainsKey(peerId))
        {
            GD.PushWarning($"[AUTH] Peer duplicado rejeitado: {peerId}");
            RejectPeer(peerId, "INVALID_REQUEST");
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        _pendingConnections[peerId] = new PendingConnection
        {
            PeerId = peerId,
            State = ConnectionAuthState.AwaitingAuthentication,
            ConnectedAt = now,
            AuthenticationDeadline = now.AddSeconds(_authenticationTimeoutSeconds),
        };
        GD.Print($"[AUTH] Peer conectado e aguardando autenticação: {peerId}");
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void SubmitGameSessionToken(string sessionToken)
    {
        if (!RunningAsServer || !Multiplayer.IsServer())
        {
            GD.PushWarning("[AUTH] RPC de autenticação recebido fora do servidor.");
            return;
        }

        int senderId = Multiplayer.GetRemoteSenderId();
        if (!_pendingConnections.TryGetValue(senderId, out PendingConnection? pending))
        {
            GD.PushWarning($"[AUTH] Token recebido de peer desconhecido: {senderId}");
            RejectPeer(senderId, "INVALID_REQUEST");
            return;
        }

        if (pending.State != ConnectionAuthState.AwaitingAuthentication)
        {
            GD.PushWarning($"[AUTH] Tentativa repetida do peer {senderId}");
            RejectPeer(senderId, "MULTIPLE_AUTH_ATTEMPTS");
            return;
        }

        if (string.IsNullOrWhiteSpace(sessionToken)
            || sessionToken.Length > MaximumSessionTokenLength)
        {
            GD.PushWarning($"[AUTH] Token vazio ou grande demais do peer {senderId}");
            RejectPeer(senderId, "INVALID_REQUEST");
            return;
        }

        pending.State = ConnectionAuthState.Validating;
        GD.Print($"[AUTH] Token recebido; validando peer {senderId}");
        _ = ValidatePeerAsync(senderId, sessionToken);
    }

    private async Task ValidatePeerAsync(int peerId, string sessionToken)
    {
        if (_gameBackendClient is null || _serverLifetime is null)
        {
            RejectPeer(peerId, "AUTH_SERVICE_UNAVAILABLE");
            return;
        }

        GameSessionValidationResult result;
        try
        {
            result = await _gameBackendClient.ValidateSessionAsync(
                sessionToken,
                _serverLifetime.Token);
        }
        catch (OperationCanceledException)
        {
            if (_pendingConnections.ContainsKey(peerId))
                RejectPeer(peerId, "AUTH_SERVICE_UNAVAILABLE");
            return;
        }
        catch (HttpRequestException exception)
        {
            GD.PrintErr($"[AUTH] API indisponível para peer {peerId}: {exception.Message}");
            RejectPeer(peerId, "AUTH_SERVICE_UNAVAILABLE");
            return;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[AUTH] Resposta inválida da API para peer {peerId}: {exception.Message}");
            RejectPeer(peerId, "AUTH_SERVICE_UNAVAILABLE");
            return;
        }

        if (!_pendingConnections.TryGetValue(peerId, out PendingConnection? pending)
            || pending.State != ConnectionAuthState.Validating)
        {
            GD.Print($"[AUTH] Peer {peerId} desconectou durante a validação.");
            return;
        }

        if (!result.Valid || result.Character is null)
        {
            RejectPeer(peerId, result.ErrorCode ?? "AUTHENTICATION_FAILED");
            return;
        }

        AuthenticatedCharacterData character = result.Character;
        if (_onlineCharacters.ContainsKey(character.CharacterId))
        {
            GD.Print($"[AUTH] Personagem já online rejeitado: {character.CharacterId}");
            RejectPeer(peerId, "CHARACTER_ALREADY_ONLINE");
            return;
        }

        pending.State = ConnectionAuthState.Authenticated;
        pending.CharacterId = character.CharacterId.ToString("D");
        _authenticatedPeers[peerId] = character;
        _onlineCharacters[character.CharacterId] = peerId;
        _pendingConnections.Remove(peerId);

        if (!SpawnPlayer(peerId, character))
        {
            _authenticatedPeers.Remove(peerId);
            _onlineCharacters.Remove(character.CharacterId);
            RejectPeer(peerId, "PLAYER_SPAWN_FAILED");
            return;
        }

        RpcId(
            peerId,
            MethodName.AuthenticationSucceeded,
            character.CharacterId.ToString("D"),
            character.CharacterName);
        GD.Print(
            $"[AUTH] Personagem autenticado: peer {peerId}, personagem {character.CharacterId}");
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void AuthenticationSucceeded(string characterId, string characterName)
    {
        if (RunningAsServer)
            return;

        _pendingSessionToken = null;
        SetClientStatus($"Autenticado como {characterName}.", isError: false);
        _authRoot?.CompleteGameAuthentication();
        GD.Print($"[AUTH] Autenticação concluída para personagem {characterId}");
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void AuthenticationFailed(string errorCode)
    {
        if (RunningAsServer)
            return;

        _pendingSessionToken = null;
        SetClientStatus($"Autenticação rejeitada: {errorCode}", isError: true);
        SetAuthenticationUiBusy(false);
        GD.PrintErr($"[AUTH] Autenticação rejeitada: {errorCode}");
    }

    private void RejectPeer(int peerId, string errorCode)
    {
        if (!RunningAsServer)
            return;

        if (_pendingConnections.TryGetValue(peerId, out PendingConnection? pending))
            pending.State = ConnectionAuthState.Rejected;

        GD.Print($"[AUTH] Autenticação rejeitada para peer {peerId}: {errorCode}");
        if (peerId > NetworkConstants.ServerPeerId)
        {
            RpcId(peerId, MethodName.AuthenticationFailed, errorCode);
            _ = DisconnectRejectedPeerAsync(peerId);
        }
    }

    private async Task DisconnectRejectedPeerAsync(int peerId)
    {
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        if (_pendingConnections.ContainsKey(peerId))
            Multiplayer.MultiplayerPeer.DisconnectPeer(peerId);
    }

    private void HandleAuthenticatedPeerDisconnected(int peerId)
    {
        _pendingConnections.Remove(peerId);

        if (!_authenticatedPeers.Remove(peerId, out AuthenticatedCharacterData? character))
            return;

        _onlineCharacters.Remove(character.CharacterId);
        Player? player = _players?.GetNodeOrNull<Player>(peerId.ToString());
        if (player?.TryCaptureAuthenticatedState(out AuthenticatedPlayerState? state) == true
            && state is not null)
        {
            _ = PersistDisconnectedPlayerAsync(state);
        }
    }

    private async Task PersistDisconnectedPlayerAsync(AuthenticatedPlayerState state)
    {
        if (_characterPersistenceService is null || _serverLifetime is null)
            return;

        try
        {
            bool saved = await _characterPersistenceService.SaveCharacterStateAsync(
                state,
                _serverLifetime.Token);
            if (!saved)
                GD.PrintErr($"[PERSISTENCE] Falha ao salvar personagem {state.CharacterId}");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException)
        {
            GD.PrintErr(
                $"[PERSISTENCE] API indisponível ao salvar {state.CharacterId}: {exception.Message}");
        }
    }

    private void BuildAuthenticatedClientInterface()
    {
        try
        {
            _gameConnection = new GameConnectionManager();
            PackedScene authScene = GD.Load<PackedScene>(
                "res://scenes/UI/Auth/AuthRoot.tscn");
            _authRoot = authScene.Instantiate<AuthRoot>();
            _authRoot.Name = "AuthRoot";
            _authRoot.SessionCreated += BeginGameConnection;
            CanvasLayer clientUi = new() { Name = "ClientUI", Layer = 100 };
            AddChild(clientUi);
            clientUi.AddChild(_authRoot);
        }
        catch (ArgumentException exception)
        {
            GD.PushError($"[CLIENT][API] {exception.Message}");
            return;
        }

        if (HudScene is not null)
        {
            Node hud = HudScene.Instantiate();
            hud.Name = "HUD";
            AddChild(hud);
        }
        else
        {
            GD.PushWarning("[CLIENT] Cena do HUD não configurada.");
        }
    }

    private void BeginGameConnection(CreateGameSessionApiResponse session)
    {
        if (_gameConnection is null)
            return;

        _pendingSessionToken = session.SessionToken;
        _pendingSessionExpiration = session.ExpiresAt;
        Error error = _gameConnection.Connect(
            Multiplayer,
            session.GameServerHost,
            session.GameServerPort);
        if (error == Error.Ok)
        {
            SetClientStatus(
                $"Conectando a {session.GameServerHost}:{session.GameServerPort}...",
                isError: false);
            return;
        }

        _pendingSessionToken = null;
        _authRoot?.ShowAfterConnectionFailure(
            "Não foi possível conectar ao servidor do jogo.");
    }

    private async Task LoginAndLoadCharactersAsync()
    {
        if (_authManager is null || _emailInput is null || _passwordInput is null)
            return;

        SetAuthenticationUiBusy(true);
        SetClientStatus("Autenticando na API...", isError: false);
        try
        {
            await _authManager.LoginAsync(_emailInput.Text.Trim(), _passwordInput.Text);
            _passwordInput.Text = string.Empty;
            await RefreshCharactersAsync();
            SetClientStatus($"Login realizado: {_authManager.Username}", isError: false);
        }
        catch (Exception exception) when (
            exception is ApiRequestException or HttpRequestException or TaskCanceledException)
        {
            _authManager.Logout();
            SetClientStatus($"Falha no login: {exception.Message}", isError: true);
        }
        finally
        {
            SetAuthenticationUiBusy(false);
        }
    }

    private async Task RefreshCharactersAsync()
    {
        if (_characterSelection is null || _characterOptions is null)
            return;

        try
        {
            IReadOnlyList<CharacterApiResponse> characters =
                await _characterSelection.RefreshAsync();
            _characterOptions.Clear();
            foreach (CharacterApiResponse character in characters)
            {
                _characterOptions.AddItem(
                    $"{character.Name} - Nv. {character.Level}");
                int index = _characterOptions.ItemCount - 1;
                _characterOptions.SetItemMetadata(index, character.Id.ToString("D"));
            }

            if (_enterGameButton is not null)
                _enterGameButton.Disabled = characters.Count == 0;
            if (_refreshCharactersButton is not null)
                _refreshCharactersButton.Disabled = false;
            if (_createCharacterButton is not null)
                _createCharacterButton.Disabled = false;
        }
        catch (Exception exception) when (
            exception is ApiRequestException or HttpRequestException or TaskCanceledException)
        {
            SetClientStatus($"Falha ao listar personagens: {exception.Message}", isError: true);
        }
    }

    private async Task CreateCharacterAsync()
    {
        if (_characterSelection is null || _characterNameInput is null)
            return;

        string name = _characterNameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetClientStatus("Informe o nome do personagem.", isError: true);
            return;
        }

        SetAuthenticationUiBusy(true);
        try
        {
            CharacterApiResponse created = await _characterSelection.CreateAsync(name);
            _characterNameInput.Text = string.Empty;
            await RefreshCharactersAsync();
            SelectCharacterOption(created.Id);
            SetClientStatus($"Personagem {created.Name} criado.", isError: false);
        }
        catch (Exception exception) when (
            exception is ApiRequestException or HttpRequestException or TaskCanceledException)
        {
            SetClientStatus($"Falha ao criar personagem: {exception.Message}", isError: true);
        }
        finally
        {
            SetAuthenticationUiBusy(false);
        }
    }

    private async Task CreateSessionAndConnectAsync()
    {
        if (_characterSelection is null || _gameConnection is null)
            return;

        SetAuthenticationUiBusy(true);
        SetClientStatus("Criando sessão temporária...", isError: false);
        try
        {
            CreateGameSessionApiResponse session =
                await _characterSelection.CreateGameSessionAsync();
            _pendingSessionToken = session.SessionToken;
            _pendingSessionExpiration = session.ExpiresAt;

            Error error = _gameConnection.Connect(
                Multiplayer,
                session.GameServerHost,
                session.GameServerPort);
            if (error != Error.Ok)
                throw new InvalidOperationException($"Falha ao criar conexão ENet: {error}");

            SetClientStatus(
                $"Conectando a {session.GameServerHost}:{session.GameServerPort}...",
                isError: false);
        }
        catch (Exception exception) when (
            exception is ApiRequestException
                or HttpRequestException
                or TaskCanceledException
                or InvalidOperationException)
        {
            _pendingSessionToken = null;
            SetAuthenticationUiBusy(false);
            SetClientStatus($"Falha ao entrar: {exception.Message}", isError: true);
        }
    }

    private void SubmitPendingSessionToken()
    {
        int peerId = Multiplayer.GetUniqueId();
        if (string.IsNullOrWhiteSpace(_pendingSessionToken)
            || _pendingSessionExpiration <= DateTimeOffset.UtcNow)
        {
            SetClientStatus("Sessão temporária ausente ou expirada.", isError: true);
            _gameConnection?.Close();
            SetAuthenticationUiBusy(false);
            return;
        }

        RpcId(
            NetworkConstants.ServerPeerId,
            MethodName.SubmitGameSessionToken,
            _pendingSessionToken);
        SetClientStatus($"ENet conectado (peer {peerId}); autenticando...", isError: false);
        GD.Print($"[AUTH] Token temporário enviado pelo peer {peerId}");
    }

    private void OnCharacterSelected(long index)
    {
        if (_characterOptions is null || _characterSelection is null)
            return;

        Variant metadata = _characterOptions.GetItemMetadata((int)index);
        if (Guid.TryParse(metadata.AsString(), out Guid characterId))
            _characterSelection.Select(characterId);
    }

    private void SelectCharacterOption(Guid characterId)
    {
        if (_characterOptions is null || _characterSelection is null)
            return;

        for (int index = 0; index < _characterOptions.ItemCount; index++)
        {
            if (!Guid.TryParse(
                    _characterOptions.GetItemMetadata(index).AsString(),
                    out Guid itemId)
                || itemId != characterId)
            {
                continue;
            }

            _characterOptions.Select(index);
            _characterSelection.Select(characterId);
            break;
        }
    }

    private void SetAuthenticationUiBusy(bool busy)
    {
        if (_loginButton is not null)
            _loginButton.Disabled = busy;
        if (_refreshCharactersButton is not null)
            _refreshCharactersButton.Disabled = busy || _authManager?.IsAuthenticated != true;
        if (_createCharacterButton is not null)
            _createCharacterButton.Disabled = busy || _authManager?.IsAuthenticated != true;
        if (_enterGameButton is not null)
        {
            _enterGameButton.Disabled =
                busy || _characterSelection?.SelectedCharacter is null;
        }
    }

    private void ResetAuthenticatedClientConnection(string message)
    {
        _gameConnection?.Close();
        _pendingSessionToken = null;
        SetAuthenticationUiBusy(false);
        _authRoot?.ShowAfterConnectionFailure(message);
        SetClientStatus(message, isError: true);
    }

    private void DisposeAuthenticationResources()
    {
        _serverLifetime?.Cancel();
        _serverLifetime?.Dispose();
        _serverLifetime = null;
        _gameBackendClient?.Dispose();
        _gameBackendClient = null;
        _apiClient?.Dispose();
        _apiClient = null;
        _gameConnection?.Dispose();
        _gameConnection = null;
        _pendingSessionToken = null;
        if (_authRoot is not null)
            _authRoot.SessionCreated -= BeginGameConnection;
    }

    private static string GetEnvironmentOrDefault(string name, string fallback)
    {
        string? value = System.Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int GetPositiveEnvironmentInt(
        string name,
        int fallback,
        int minimum,
        int maximum)
    {
        return int.TryParse(System.Environment.GetEnvironmentVariable(name), out int value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }
}
