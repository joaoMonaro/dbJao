using Godot;
using System;
using System.Net.Mail;

public partial class RegisterScreen : VBoxContainer
{
    public event Action<RegisterApiRequest>? SubmitRequested;
    public event Action? BackRequested;
    private LineEdit _username = null!;
    private LineEdit _email = null!;
    private PasswordField _password = null!;
    private PasswordField _confirmation = null!;
    private CheckBox _terms = null!;
    private Button _submit = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        AddChild(AuthUi.Title("Criar conta"));
        _username = AuthUi.Input("Nome de usuário");
        _email = AuthUi.Input("Email");
        _password = new PasswordField();
        _confirmation = new PasswordField();
        AddChild(_username); AddChild(_email); AddChild(_password); AddChild(_confirmation);
        _terms = new CheckBox { Text = "Li e aceito os termos de uso" };
        AddChild(_terms);
        _submit = AuthUi.PrimaryButton("Criar conta");
        _submit.Pressed += Submit;
        AddChild(_submit);
        Button back = AuthUi.LinkButton("Voltar para o login");
        back.Pressed += () => BackRequested?.Invoke();
        AddChild(back);
        _status = AuthUi.Status();
        AddChild(_status);
        _confirmation.Input.TextSubmitted += _ => Submit();
    }

    public void Activate()
    {
        _status.Text = "";
        _username.CallDeferred(Control.MethodName.GrabFocus);
    }

    public void SetBusy(bool busy)
    {
        foreach (LineEdit field in new[] { _username, _email })
            field.Editable = !busy;
        _password.SetEditable(!busy);
        _confirmation.SetEditable(!busy);
        _terms.Disabled = busy;
        _submit.Disabled = busy;
        _submit.Text = busy ? "Criando conta..." : "Criar conta";
    }

    public void SetStatus(string message, bool error)
    {
        _status.Text = message;
        _status.Modulate = error ? new Color(1, .45f, .45f) : Colors.White;
    }

    private void Submit()
    {
        string username = _username.Text.Trim();
        string email = _email.Text.Trim().ToLowerInvariant();
        string? error = username.Length is < 3 or > 20
            ? "O nome deve ter entre 3 e 20 caracteres."
            : email.Length == 0 ? "Preencha seu email."
            : !IsEmail(email) ? "Informe um email válido."
            : _password.Text.Length < 8 ? "A senha deve ter pelo menos 8 caracteres."
            : _password.Text != _confirmation.Text ? "As senhas não coincidem."
            : !_terms.ButtonPressed ? "Aceite os termos para continuar."
            : null;
        if (error is not null) { SetStatus(error, true); return; }
        SubmitRequested?.Invoke(new RegisterApiRequest(username, email, _password.Text));
    }

    private static bool IsEmail(string email)
    {
        try { return new MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }
}
