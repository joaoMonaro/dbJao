using Godot;
using System;

public partial class ForgotPasswordScreen : VBoxContainer
{
    public event Action<string>? SubmitRequested;
    public event Action? BackRequested;
    public event Action<string, string>? ResetRequested;
    private LineEdit _email = null!;
    private Button _submit = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        AddChild(AuthUi.Title("Recuperar senha"));
        AddChild(new Label
        {
            Text = "Informe seu email para receber as instruções.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        _email = AuthUi.Input("Email");
        AddChild(_email);
        _submit = AuthUi.PrimaryButton("Enviar instruções");
        _submit.Pressed += Submit;
        AddChild(_submit);
        Button reset = AuthUi.LinkButton("Já tenho um token");
        reset.Pressed += () => ResetRequested?.Invoke(_email.Text.Trim(), "");
        AddChild(reset);
        Button back = AuthUi.LinkButton("Voltar para o login");
        back.Pressed += () => BackRequested?.Invoke();
        AddChild(back);
        _status = AuthUi.Status();
        AddChild(_status);
        _email.TextSubmitted += _ => Submit();
    }

    public void Activate(string email = "")
    {
        _email.Text = email;
        _status.Text = "";
        _email.CallDeferred(Control.MethodName.GrabFocus);
    }
    public void SetBusy(bool busy)
    {
        _email.Editable = !busy;
        _submit.Disabled = busy;
        _submit.Text = busy ? "Enviando..." : "Enviar instruções";
    }
    public void SetStatus(string message, bool error)
    {
        _status.Text = message;
        _status.Modulate = error ? new Color(1, .45f, .45f) : Colors.White;
    }
    private void Submit()
    {
        string email = _email.Text.Trim().ToLowerInvariant();
        if (email.Length == 0) { SetStatus("Preencha seu email.", true); return; }
        SubmitRequested?.Invoke(email);
    }
}
