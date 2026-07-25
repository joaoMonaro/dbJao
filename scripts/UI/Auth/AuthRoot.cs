using Godot;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public enum AuthScreenType { Login, Register, ForgotPassword, ResetPassword }

public partial class AuthRoot : Control
{
    public event Action<CreateGameSessionApiResponse>? SessionCreated;
    private readonly ApiClient _apiClient =
        new(ClientConfiguration.ApiBaseUrl, ClientConfiguration.RequestTimeoutSeconds);
    private AuthManager _authManager = null!;
    private AuthService _authService = null!;
    private CharacterSelectionManager _characters = null!;
    private Control _screenContainer = null!;
    private LoadingOverlay _loading = null!;
    private AcceptDialog _errorDialog = null!;
    private LoginScreen _login = null!;
    private RegisterScreen _register = null!;
    private ForgotPasswordScreen _forgot = null!;
    private ResetPasswordScreen _reset = null!;
    private CharacterSelectionScreen _characterSelection = null!;
    private Control? _current;
    private CancellationTokenSource? _operation;
    private bool _busy;

    public override void _Ready()
    {
        _authManager = new AuthManager(_apiClient);
        _authService = new AuthService(_apiClient);
        _characters = new CharacterSelectionManager(_apiClient, _authManager);
        _screenContainer = GetNode<Control>("%ScreenContainer");
        _loading = GetNode<LoadingOverlay>("%LoadingOverlay");
        _errorDialog = GetNode<AcceptDialog>("%ErrorDialog");
        _login = LoadScreen<LoginScreen>("res://scenes/UI/Auth/LoginScreen.tscn");
        _register = LoadScreen<RegisterScreen>("res://scenes/UI/Auth/RegisterScreen.tscn");
        _forgot = LoadScreen<ForgotPasswordScreen>(
            "res://scenes/UI/Auth/ForgotPasswordScreen.tscn");
        _reset = LoadScreen<ResetPasswordScreen>(
            "res://scenes/UI/Auth/ResetPasswordScreen.tscn");
        _characterSelection = LoadScreen<CharacterSelectionScreen>(
            "res://scenes/UI/CharacterSelection/CharacterSelectionScreen.tscn");
        _characterSelection.Initialize(_characters);
        WireEvents();
        ShowScreen(AuthScreenType.Login);
    }

    public override void _ExitTree()
    {
        CancelOperation();
        _apiClient.Dispose();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("ui_cancel") && !_busy)
        {
            if (_current == _register || _current == _forgot || _current == _reset)
                ShowScreen(AuthScreenType.Login);
        }
    }

    public void ShowScreen(AuthScreenType screenType)
    {
        CancelOperation();
        Control next = screenType switch
        {
            AuthScreenType.Login => _login,
            AuthScreenType.Register => _register,
            AuthScreenType.ForgotPassword => _forgot,
            AuthScreenType.ResetPassword => _reset,
            _ => _login,
        };
        ShowOnly(next);
        switch (screenType)
        {
            case AuthScreenType.Login: _login.Activate(); break;
            case AuthScreenType.Register: _register.Activate(); break;
            case AuthScreenType.ForgotPassword: _forgot.Activate(); break;
            case AuthScreenType.ResetPassword: _reset.Activate(); break;
        }
        GD.Print($"[AUTH UI] Tela aberta: {screenType}");
    }

    public void ShowAfterConnectionFailure(string message)
    {
        Show();
        ShowOnly(_characterSelection);
        _errorDialog.DialogText = message;
        _errorDialog.PopupCentered();
    }

    public void CompleteGameAuthentication() => Hide();

    private T LoadScreen<T>(string path) where T : Control
    {
        T screen = GD.Load<PackedScene>(path).Instantiate<T>();
        screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        screen.Hide();
        _screenContainer.AddChild(screen);
        return screen;
    }

    private void WireEvents()
    {
        _login.LoginRequested += (email, password) => _ = LoginAsync(email, password);
        _login.RegisterRequested += () => ShowScreen(AuthScreenType.Register);
        _login.ForgotPasswordRequested += () => ShowScreen(AuthScreenType.ForgotPassword);
        _register.BackRequested += () => ShowScreen(AuthScreenType.Login);
        _register.SubmitRequested += request => _ = RegisterAsync(request);
        _forgot.BackRequested += () => ShowScreen(AuthScreenType.Login);
        _forgot.SubmitRequested += email => _ = ForgotAsync(email);
        _forgot.ResetRequested += (email, token) =>
        {
            ShowOnly(_reset);
            _reset.Activate(email, token);
        };
        _reset.BackRequested += () => ShowScreen(AuthScreenType.ForgotPassword);
        _reset.SubmitRequested += request => _ = ResetAsync(request);
        _characterSelection.LogoutRequested += Logout;
        _characterSelection.SessionCreated += session => SessionCreated?.Invoke(session);
    }

    private async Task LoginAsync(string email, string password)
    {
        if (!BeginOperation("Entrando...")) return;
        _login.SetBusy(true);
        GD.Print("[AUTH] Login iniciado");
        try
        {
            await _authManager.LoginAsync(email, password, _operation!.Token);
            _login.ClearPassword();
            ShowOnly(_characterSelection);
            await _characterSelection.ActivateAsync(_operation.Token);
            GD.Print("[AUTH] Login concluído");
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            _authManager.Logout();
            _login.ClearPassword();
            _login.SetStatus(AuthErrorMessages.From(exception), true);
            GD.Print("[AUTH] Erro de conexão");
        }
        finally { _login.SetBusy(false); EndOperation(); }
    }

    private async Task RegisterAsync(RegisterApiRequest request)
    {
        if (!BeginOperation("Criando conta...")) return;
        _register.SetBusy(true);
        try
        {
            await _authService.RegisterAsync(request, _operation!.Token);
            GD.Print("[AUTH] Cadastro concluído");
            ShowOnly(_login);
            _login.Activate(
                request.Email,
                "Conta criada com sucesso. Agora você pode entrar.");
        }
        catch (Exception exception) when (IsExpected(exception))
        { _register.SetStatus(AuthErrorMessages.From(exception), true); }
        finally { _register.SetBusy(false); EndOperation(); }
    }

    private async Task ForgotAsync(string email)
    {
        if (!BeginOperation("Enviando instruções...")) return;
        _forgot.SetBusy(true);
        try
        {
            ForgotPasswordApiResponse response = await _authService.ForgotPasswordAsync(
                new ForgotPasswordApiRequest(email), _operation!.Token);
            _forgot.SetStatus(
                "Se existir uma conta com esse email, você receberá as instruções para redefinir sua senha.",
                false);
            GD.Print("[AUTH] Recuperação solicitada");
            if (!string.IsNullOrWhiteSpace(response.DevelopmentToken))
            {
                ShowOnly(_reset);
                _reset.Activate(email, response.DevelopmentToken);
            }
        }
        catch (Exception exception) when (IsExpected(exception))
        { _forgot.SetStatus(AuthErrorMessages.From(exception), true); }
        finally { _forgot.SetBusy(false); EndOperation(); }
    }

    private async Task ResetAsync(ResetPasswordApiRequest request)
    {
        if (!BeginOperation("Redefinindo senha...")) return;
        _reset.SetBusy(true);
        try
        {
            await _authService.ResetPasswordAsync(request, _operation!.Token);
            ShowOnly(_login);
            _login.Activate(request.Email, "Senha redefinida com sucesso.");
        }
        catch (Exception exception) when (IsExpected(exception))
        { _reset.SetStatus(AuthErrorMessages.From(exception), true); }
        finally { _reset.SetBusy(false); EndOperation(); }
    }

    private void Logout()
    {
        _authManager.Logout();
        ShowScreen(AuthScreenType.Login);
    }

    private void ShowOnly(Control screen)
    {
        foreach (Node child in _screenContainer.GetChildren())
            if (child is Control control) control.Visible = control == screen;
        _current = screen;
    }

    private bool BeginOperation(string message)
    {
        if (_busy) return false;
        CancelOperation();
        _busy = true;
        _operation = new CancellationTokenSource();
        _loading.ShowMessage(message);
        return true;
    }

    private void EndOperation()
    {
        _loading.Hide();
        _operation?.Dispose();
        _operation = null;
        _busy = false;
    }

    private void CancelOperation()
    {
        _operation?.Cancel();
        _operation?.Dispose();
        _operation = null;
        _loading?.Hide();
        _busy = false;
    }

    private static bool IsExpected(Exception exception) =>
        exception is ApiRequestException or HttpRequestException
            or TaskCanceledException or OperationCanceledException;
}
