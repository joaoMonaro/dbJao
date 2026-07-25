using Godot;
using System;

public partial class LoginScreen : VBoxContainer
{
    public event Action<string, string>? LoginRequested;
    public event Action? RegisterRequested;
    public event Action? ForgotPasswordRequested;

    private LineEdit _email = null!;
    private PasswordField _password = null!;
    private Button _submit = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        AddChild(AuthUi.Title("Entrar no jogo"));
        _email = AuthUi.Input("Email");
        AddChild(_email);
        _password = new PasswordField();
        AddChild(_password);
        CheckBox remember = new()
        {
            Text = "Lembrar de mim (em breve)",
            Disabled = true,
        };
        AddChild(remember);
        _submit = AuthUi.PrimaryButton("Entrar");
        _submit.Pressed += Submit;
        AddChild(_submit);
        Button forgot = AuthUi.LinkButton("Esqueci minha senha");
        forgot.Pressed += () => ForgotPasswordRequested?.Invoke();
        AddChild(forgot);
        Button register = AuthUi.LinkButton("Criar conta");
        register.Pressed += () => RegisterRequested?.Invoke();
        AddChild(register);
        _status = AuthUi.Status();
        AddChild(_status);
        AddChild(new Label
        {
            Text = $"v{ClientConfiguration.Version}",
            HorizontalAlignment = HorizontalAlignment.Right,
            Modulate = new Color(0.72f, 0.75f, 0.82f),
        });
        _email.TextSubmitted += _ => _password.Input.GrabFocus();
        _password.Input.TextSubmitted += _ => Submit();
    }

    public void Activate(string email = "", string message = "")
    {
        if (!string.IsNullOrWhiteSpace(email))
            _email.Text = email;
        SetStatus(message, false);
        _email.CallDeferred(Control.MethodName.GrabFocus);
    }

    public void SetBusy(bool busy)
    {
        _email.Editable = !busy;
        _password.SetEditable(!busy);
        _submit.Disabled = busy;
        _submit.Text = busy ? "Entrando..." : "Entrar";
    }

    public void ClearPassword() => _password.Text = string.Empty;
    public void SetStatus(string message, bool error)
    {
        _status.Text = message;
        _status.Modulate = error ? new Color(1, .45f, .45f) : Colors.White;
    }

    private void Submit()
    {
        string email = _email.Text.Trim().ToLowerInvariant();
        if (email.Length == 0) { SetStatus("Preencha seu email.", true); _email.GrabFocus(); return; }
        if (_password.Text.Length == 0) { SetStatus("Preencha sua senha.", true); _password.Input.GrabFocus(); return; }
        LoginRequested?.Invoke(email, _password.Text);
    }
}
