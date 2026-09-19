using Godot;

public partial class Hud : CanvasLayer
{
    [Export] public StringName PlayerGroup { get; set; } = new("player");

    private ProgressBar _healthBar = null!;
    private ProgressBar _manaBar = null!;
    private ProgressBar _experienceBar = null!;
    private Label _experienceLabel = null!;
    private Label _progressionLabel = null!;
    private Label _battlePowerLabel = null!;
    private Label _combatStatsLabel = null!;
    private Label _healthLabel = null!;
    private TextureButton _mapButton = null!;
    private Control _mapModal = null!;
    private Button _closeMapButton = null!;
    private Button _cleanPathButton = null!;
    private Button _kameHouseButton = null!;
    private PanelContainer _debugCommandsPanel = null!;
    private Label _debugCommandOutput = null!;
    private LineEdit _debugCommandInput = null!;
    private Player? _boundPlayer;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/HealthBar"
        );
        _manaBar = GetNode<ProgressBar>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/ManaBar"
        );
        _experienceBar = GetNode<ProgressBar>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/ExperienceBar"
        );
        _experienceLabel = GetNode<Label>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/ExperienceBar/Label"
        );
        _progressionLabel = GetNode<Label>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/ProgressionLabel"
        );
        _battlePowerLabel = GetNode<Label>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/BattlePowerLabel"
        );
        _combatStatsLabel = GetNode<Label>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/CombatStatsLabel"
        );
        _healthLabel = GetNode<Label>(
            "MarginContainer/PanelContainer/HBoxContainer/Bars/HealthBar/Label"
        );
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
        _debugCommandInput.TextSubmitted += SubmitDebugCommand;
        _debugCommandsPanel.Visible = OS.IsDebugBuild();

        GetTree().NodeAdded += OnNodeAdded;
        CallDeferred(MethodName.TryBindLocalPlayer);
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
        _debugCommandInput.TextSubmitted -= SubmitDebugCommand;
        UnbindPlayer();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (!_debugCommandsPanel.Visible || input is not InputEventKey key
            || !key.Pressed || key.Echo)
            return;

        if (key.Keycode == Key.Slash && !_debugCommandInput.HasFocus())
        {
            _debugCommandInput.Text = "/";
            _debugCommandInput.CaretColumn = 1;
            _debugCommandInput.GrabFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape && _debugCommandInput.HasFocus())
        {
            _debugCommandInput.ReleaseFocus();
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
            player.ProgressionChanged += OnProgressionChanged;
            player.BattlePowerChanged += OnBattlePowerChanged;
            player.CombatStatsChanged += OnCombatStatsChanged;
            player.DebugCommandResult += OnDebugCommandResult;
            player.TreeExiting += OnBoundPlayerExiting;

            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
            OnManaChanged(player.CurrentMana, player.MaxMana);
            OnExperienceChanged(player.CurrentExperience, player.MaxExperience);
            OnProgressionChanged(player.Level, player.Reset);
            OnBattlePowerChanged(player.BaseBattlePower);
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
            _boundPlayer = null;
            return;
        }

        _boundPlayer!.HealthChanged -= OnHealthChanged;
        _boundPlayer.ManaChanged -= OnManaChanged;
        _boundPlayer.ExperienceChanged -= OnExperienceChanged;
        _boundPlayer.ProgressionChanged -= OnProgressionChanged;
        _boundPlayer.BattlePowerChanged -= OnBattlePowerChanged;
        _boundPlayer.CombatStatsChanged -= OnCombatStatsChanged;
        _boundPlayer.DebugCommandResult -= OnDebugCommandResult;
        _boundPlayer.TreeExiting -= OnBoundPlayerExiting;
        _boundPlayer = null;
    }

    private void OnHealthChanged(int current, int maximum)
    {
        _healthBar.MaxValue = Mathf.Max(maximum, 1);
        _healthBar.Value = current;
        _healthLabel.Text = $"{current}/{maximum}";
    }

    private void OnManaChanged(int current, int maximum)
    {
        _manaBar.MaxValue = Mathf.Max(maximum, 1);
        _manaBar.Value = current;
    }

    private void OnExperienceChanged(long current, long maximum)
    {
        _experienceBar.MaxValue = maximum > 0 ? maximum : 1;
        _experienceBar.Value = current;
        _experienceLabel.Text = $"{current}/{maximum} XP";
    }

    private void OnProgressionChanged(int level, long reset)
    {
        _progressionLabel.Text = $"Nível {level} | Reset {reset}";
    }

    private void OnBattlePowerChanged(long baseBattlePower)
    {
        _battlePowerLabel.Text = $"Poder de Luta: {baseBattlePower:N0}";
    }

    private void OnCombatStatsChanged(
        string characterName,
        long attack,
        long defense,
        long kiAttack)
    {
        _combatStatsLabel.Text =
            $"Personagem: {characterName}\nATQ {attack:N0} | DEF {defense:N0} | KI {kiAttack:N0}";
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
