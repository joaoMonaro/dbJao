using Godot;
using System;

public partial class ResetPasswordScreen : VBoxContainer
{
    public event Action<ResetPasswordApiRequest>? SubmitRequested;
    public event Action? BackRequested;
    private LineEdit _email = null!;
    private LineEdit _token = null!;
    private PasswordField _password = null!;
    private PasswordField _confirmation = null!;
    private Button _submit = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        AddChild(AuthUi.Title("Redefinir senha"));
        _email = AuthUi.Input("Email");
        _token = AuthUi.Input("Token de recuperação");
        _password = new PasswordField();
        _confirmation = new PasswordField();
        AddChild(_email); AddChild(_token); AddChild(_password); AddChild(_confirmation);
        _submit = AuthUi.PrimaryButton("Redefinir senha");
        _submit.Pressed += Submit;
        AddChild(_submit);
        Button back = AuthUi.LinkButton("Voltar");
        back.Pressed += () => BackRequested?.Invoke();
        AddChild(back);
        _status = AuthUi.Status();
        AddChild(_status);
        _confirmation.Input.TextSubmitted += _ => Submit();
    }

    public void Activate(string email = "", string token = "")
    {
        _email.Text = email; _token.Text = token; _status.Text = "";
        (_email.Text.Length == 0 ? _email : _token).CallDeferred(Control.MethodName.GrabFocus);
    }
    public void SetBusy(bool busy)
    {
        _email.Editable = _token.Editable = !busy;
        _password.SetEditable(!busy); _confirmation.SetEditable(!busy);
        _submit.Disabled = busy;
        _submit.Text = busy ? "Redefinindo..." : "Redefinir senha";
    }
    public void SetStatus(string message, bool error)
    {
        _status.Text = message;
        _status.Modulate = error ? new Color(1, .45f, .45f) : Colors.White;
    }
    private void Submit()
    {
        string email = _email.Text.Trim().ToLowerInvariant();
        string token = _token.Text.Trim();
        string? error = email.Length == 0 ? "Preencha seu email."
            : token.Length == 0 ? "Informe o token de recuperação."
            : _password.Text.Length < 8 ? "A senha deve ter pelo menos 8 caracteres."
            : _password.Text != _confirmation.Text ? "As senhas não coincidem."
            : null;
        if (error is not null) { SetStatus(error, true); return; }
        SubmitRequested?.Invoke(new ResetPasswordApiRequest(email, token, _password.Text));
    }
}
