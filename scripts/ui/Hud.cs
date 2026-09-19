using Godot;

public partial class Hud : CanvasLayer
{
    [Export] public StringName PlayerGroup { get; set; } = new("player");

    private ProgressBar _healthBar = null!;
    private ProgressBar _manaBar = null!;
    private ProgressBar _experienceBar = null!;
    private Label _experienceLabel = null!;
    private Label _healthLabel = null!;
    private Label _manaLabel = null!;
    private Button _portraitButton = null!;
    private TextureRect _portrait = null!;
    private PlayerProfileModal _profileModal = null!;
    private TextureButton _mapButton = null!;
    private Control _mapModal = null!;
    private Button _closeMapButton = null!;
    private Button _cleanPathButton = null!;
    private Button _kameHouseButton = null!;
    private Button _debugCommandsToggle = null!;
    private PanelContainer _debugCommandsPanel = null!;
    private Label _debugCommandOutput = null!;
    private LineEdit _debugCommandInput = null!;
    private Player? _boundPlayer;
    private string _portraitCharacterId = string.Empty;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/HealthBar"
        );
        _manaBar = GetNode<ProgressBar>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/ManaBar"
        );
        _experienceBar = GetNode<ProgressBar>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/ExperienceBar"
        );
        _experienceLabel = GetNode<Label>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/ExperienceBar/Label"
        );
        _healthLabel = GetNode<Label>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/HealthBar/Label"
        );
        _manaLabel = GetNode<Label>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/Bars/ManaBar/Label"
        );
        _portraitButton = GetNode<Button>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/PortraitButton"
        );
        _portrait = GetNode<TextureRect>(
            "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content/PortraitButton/Portrait"
        );
        _profileModal = GetNode<PlayerProfileModal>("PlayerProfileModal");
        _mapButton = GetNode<TextureButton>("MapButton");
        _mapModal = GetNode<Control>("MapModal");
        _closeMapButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/VBoxContainer/CloseButton"
        );
        _cleanPathButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/VBoxContainer/CleanPathButton"
        );
        _kameHouseButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/VBoxContainer/KameHouseButton"
        );
        _debugCommandsToggle = GetNode<Button>("DebugCommandsToggle");
        _debugCommandsPanel = GetNode<PanelContainer>("DebugCommands");
        _debugCommandOutput = GetNode<Label>(
            "DebugCommands/MarginContainer/VBoxContainer/Output"
        );
        _debugCommandInput = GetNode<LineEdit>(
            "DebugCommands/MarginContainer/VBoxContainer/Input"
        );

        _mapButton.Pressed += OpenMapModal;
        _closeMapButton.Pressed += CloseMapModal;
        _cleanPathButton.Pressed += TravelToCleanPath;
        _kameHouseButton.Pressed += TravelToKameHouse;
        _portraitButton.Pressed += OpenProfileModal;
        _debugCommandsToggle.Pressed += ToggleDebugCommands;
        _debugCommandInput.TextSubmitted += SubmitDebugCommand;
        _debugCommandsToggle.Visible = OS.IsDebugBuild();
        SetDebugCommandsExpanded(false);

        GetTree().NodeAdded += OnNodeAdded;
        CallDeferred(MethodName.TryBindLocalPlayer);
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
        _portraitButton.Pressed -= OpenProfileModal;
        _debugCommandsToggle.Pressed -= ToggleDebugCommands;
        _debugCommandInput.TextSubmitted -= SubmitDebugCommand;
        UnbindPlayer();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (_profileModal.IsOpen)
            return;

        if (!_debugCommandsToggle.Visible || input is not InputEventKey key
            || !key.Pressed || key.Echo)
            return;

        if (key.Keycode == Key.Slash && !_debugCommandInput.HasFocus())
        {
            SetDebugCommandsExpanded(true);
            _debugCommandInput.Text = "/";
            _debugCommandInput.CaretColumn = 1;
            _debugCommandInput.GrabFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape && _debugCommandsPanel.Visible)
        {
            SetDebugCommandsExpanded(false);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is Player)
            CallDeferred(MethodName.TryBindLocalPlayer);
    }

    private void TryBindLocalPlayer()
    {
        if (GodotObject.IsInstanceValid(_boundPlayer))
            return;

        int localPeerId = Multiplayer.GetUniqueId();
        foreach (Node node in GetTree().GetNodesInGroup(PlayerGroup))
        {
            if (node is not Player player || player.OwnerPeerId != localPeerId)
                continue;

            _boundPlayer = player;
            player.HealthChanged += OnHealthChanged;
            player.ManaChanged += OnManaChanged;
            player.ExperienceChanged += OnExperienceChanged;
            player.CombatStatsChanged += OnCombatStatsChanged;
            player.DebugCommandResult += OnDebugCommandResult;
            player.TreeExiting += OnBoundPlayerExiting;
            _profileModal.Bind(player);

            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
            OnManaChanged(player.CurrentMana, player.MaxMana);
            OnExperienceChanged(player.CurrentExperience, player.MaxExperience);
            CombatStats stats = player.CurrentCombatStats;
            OnCombatStatsChanged(
                player.ActiveCharacterDefinition.Name,
                stats.Attack,
                stats.Defense,
                stats.KiAttack);
            return;
        }
    }

    private void OnBoundPlayerExiting()
    {
        UnbindPlayer();
    }

    private void UnbindPlayer()
    {
        if (!GodotObject.IsInstanceValid(_boundPlayer))
        {
            _profileModal.Bind(null);
            _portrait.Texture = null;
            _portraitCharacterId = string.Empty;
            _boundPlayer = null;
            return;
        }

        _boundPlayer!.HealthChanged -= OnHealthChanged;
        _boundPlayer.ManaChanged -= OnManaChanged;
        _boundPlayer.ExperienceChanged -= OnExperienceChanged;
        _boundPlayer.CombatStatsChanged -= OnCombatStatsChanged;
        _boundPlayer.DebugCommandResult -= OnDebugCommandResult;
        _boundPlayer.TreeExiting -= OnBoundPlayerExiting;
        _profileModal.Bind(null);
        _portrait.Texture = null;
        _portraitCharacterId = string.Empty;
        _boundPlayer = null;
    }

    private void OnHealthChanged(int current, int maximum)
    {
        _healthBar.MaxValue = Mathf.Max(maximum, 1);
        _healthBar.Value = current;
        _healthLabel.Text = $"HP {current:N0} / {maximum:N0}";
    }

    private void OnManaChanged(int current, int maximum)
    {
        _manaBar.MaxValue = Mathf.Max(maximum, 1);
        _manaBar.Value = current;
        _manaLabel.Text = $"KI {current:N0} / {maximum:N0}";
    }

    private void OnExperienceChanged(long current, long maximum)
    {
        _experienceBar.MaxValue = maximum > 0 ? maximum : 1;
        _experienceBar.Value = current;
        _experienceLabel.Text = $"XP {current:N0} / {maximum:N0}";
    }

    private void OnCombatStatsChanged(
        string characterName,
        long attack,
        long defense,
        long kiAttack)
    {
        if (GodotObject.IsInstanceValid(_boundPlayer))
            LoadPortrait(_boundPlayer!.ActiveCharacterDefinition);
    }

    private void LoadPortrait(CharacterDefinition definition)
    {
        if (_portraitCharacterId == definition.Id && _portrait.Texture is not null)
            return;

        _portraitCharacterId = definition.Id;
        if (!ResourceLoader.Exists(definition.PortraitTexturePath))
        {
            GD.PushError(
                $"[HUD] Portrait não encontrado para {definition.Id}: "
                + definition.PortraitTexturePath);
            _portrait.Texture = null;
            return;
        }

        _portrait.Texture = GD.Load<Texture2D>(definition.PortraitTexturePath);
    }

    private void OpenProfileModal()
    {
        _profileModal.Open();
    }

    private void SubmitDebugCommand(string command)
    {
        if (_boundPlayer is null)
        {
            OnDebugCommandResult("Entre com um personagem antes de executar comandos.", false);
            return;
        }

        _debugCommandOutput.Text = "Executando...";
        _debugCommandOutput.Modulate = Colors.White;
        _debugCommandInput.Clear();
        _boundPlayer.SubmitDebugCommand(command);
    }

    private void OnDebugCommandResult(string message, bool success)
    {
        _debugCommandOutput.Text = message;
        _debugCommandOutput.Modulate = success
            ? new Color("b8f5b1")
            : new Color("ff9e9e");
    }

    private void ToggleDebugCommands()
    {
        SetDebugCommandsExpanded(!_debugCommandsPanel.Visible);
        if (_debugCommandsPanel.Visible)
            _debugCommandInput.GrabFocus();
    }

    private void SetDebugCommandsExpanded(bool expanded)
    {
        _debugCommandsPanel.Visible = expanded && _debugCommandsToggle.Visible;
        _debugCommandsToggle.Text = _debugCommandsPanel.Visible ? "×" : "XP";
        _debugCommandsToggle.TooltipText = _debugCommandsPanel.Visible
            ? "Recolher comandos de debug"
            : "Abrir comandos de debug de XP";

        if (!_debugCommandsPanel.Visible && _debugCommandInput.HasFocus())
            _debugCommandInput.ReleaseFocus();
    }

    private void OpenMapModal()
    {
        _cleanPathButton.Disabled = _boundPlayer is null
            || _boundPlayer.MapId == WorldMaps.CleanPath;
        _kameHouseButton.Disabled = _boundPlayer is null
            || _boundPlayer.MapId == WorldMaps.KameHouse;
        _mapModal.Visible = true;
    }

    private void CloseMapModal()
    {
        _mapModal.Visible = false;
    }

    private void TravelToCleanPath()
    {
        _boundPlayer?.TravelToMap(WorldMaps.CleanPath);
        CloseMapModal();
    }

    private void TravelToKameHouse()
    {
        _boundPlayer?.TravelToMap(WorldMaps.KameHouse);
        CloseMapModal();
    }
}
