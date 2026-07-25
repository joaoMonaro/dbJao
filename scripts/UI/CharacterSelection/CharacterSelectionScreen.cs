using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public partial class CharacterSelectionScreen : VBoxContainer
{
    public event Action<CreateGameSessionApiResponse>? SessionCreated;
    public event Action? LogoutRequested;
    private CharacterSelectionManager? _manager;
    private OptionButton _options = null!;
    private LineEdit _name = null!;
    private Button _enter = null!;
    private Button _create = null!;
    private Button _refresh = null!;
    private Label _status = null!;
    private bool _busy;

    public override void _Ready()
    {
        AddChild(AuthUi.Title("Escolha seu personagem"));
        _options = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
        _options.ItemSelected += SelectAt;
        AddChild(_options);
        _refresh = AuthUi.LinkButton("Atualizar personagens");
        _refresh.Pressed += () => _ = RefreshAsync();
        AddChild(_refresh);
        HBoxContainer row = new();
        _name = AuthUi.Input("Nome do novo personagem");
        _name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _create = new Button { Text = "Criar" };
        _create.Pressed += () => _ = CreateAsync();
        row.AddChild(_name); row.AddChild(_create); AddChild(row);
        _enter = AuthUi.PrimaryButton("Selecionar e entrar");
        _enter.Pressed += () => _ = EnterAsync();
        AddChild(_enter);
        Button logout = AuthUi.LinkButton("Sair da conta");
        logout.Pressed += () => LogoutRequested?.Invoke();
        AddChild(logout);
        _status = AuthUi.Status();
        AddChild(_status);
    }

    public void Initialize(CharacterSelectionManager manager) => _manager = manager;
    public Task ActivateAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_manager is null || _busy) return;
        SetBusy(true, "Carregando personagens...");
        try
        {
            IReadOnlyList<CharacterApiResponse> characters =
                await _manager.RefreshAsync(cancellationToken);
            _options.Clear();
            foreach (CharacterApiResponse character in characters)
            {
                _options.AddItem($"{character.Name} — Nv. {character.Level}");
                _options.SetItemMetadata(
                    _options.ItemCount - 1, character.Id.ToString("D"));
            }
            _status.Text = characters.Count == 0
                ? "Crie seu primeiro personagem para continuar."
                : $"{characters.Count} personagem(ns) encontrado(s).";
        }
        catch (Exception exception) when (
            exception is ApiRequestException or System.Net.Http.HttpRequestException
                or TaskCanceledException)
        {
            _status.Text = AuthErrorMessages.From(exception);
        }
        finally { SetBusy(false); }
    }

    private async Task CreateAsync()
    {
        if (_manager is null || _busy) return;
        string name = _name.Text.Trim();
        if (name.Length is < 3 or > 20)
        {
            _status.Text = "O nome deve ter entre 3 e 20 caracteres.";
            return;
        }
        SetBusy(true, "Criando personagem...");
        try
        {
            CharacterApiResponse created = await _manager.CreateAsync(name);
            _name.Text = "";
            Populate(_manager.Characters);
            Select(created.Id);
        }
        catch (Exception exception) { _status.Text = AuthErrorMessages.From(exception); }
        finally { SetBusy(false); }
    }

    private async Task EnterAsync()
    {
        if (_manager is null || _manager.SelectedCharacter is null || _busy) return;
        SetBusy(true, "Criando sessão de jogo...");
        try
        {
            CreateGameSessionApiResponse session =
                await _manager.CreateGameSessionAsync();
            SessionCreated?.Invoke(session);
        }
        catch (Exception exception)
        {
            _status.Text = AuthErrorMessages.From(exception);
            SetBusy(false);
        }
    }

    private void SelectAt(long index)
    {
        if (_manager is null) return;
        if (Guid.TryParse(_options.GetItemMetadata((int)index).AsString(), out Guid id))
            _manager.Select(id);
        _enter.Disabled = _busy || _manager.SelectedCharacter is null;
    }

    private void Select(Guid id)
    {
        for (int i = 0; i < _options.ItemCount; i++)
            if (_options.GetItemMetadata(i).AsString() == id.ToString("D"))
            { _options.Select(i); _manager?.Select(id); break; }
    }

    private void Populate(IReadOnlyList<CharacterApiResponse> characters)
    {
        _options.Clear();
        foreach (CharacterApiResponse character in characters)
        {
            _options.AddItem($"{character.Name} — Nv. {character.Level}");
            _options.SetItemMetadata(
                _options.ItemCount - 1, character.Id.ToString("D"));
        }
    }

    private void SetBusy(bool busy, string message = "")
    {
        _busy = busy;
        _options.Disabled = _refresh.Disabled = _create.Disabled = busy;
        _name.Editable = !busy;
        _enter.Disabled = busy || _manager?.SelectedCharacter is null;
        if (message.Length > 0) _status.Text = message;
    }
}
