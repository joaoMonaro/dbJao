using Godot;

public partial class Hud : CanvasLayer
{
    private sealed record StagePresentation(
        string Title,
        string Subtitle,
        string Description,
        string BossName,
        string? MapId);

    private static readonly StagePresentation[] Stages =
    [
        new(
            "BEAR THIEF",
            "O Ladrão da Estrada",
            "A jornada de Goku começa a ficar perigosa.\n\n"
                + "Um enorme ladrão bloqueia o caminho e ataca qualquer viajante que se aproxima.\n\n"
                + "Derrote Bear Thief e continue sua aventura.",
            "BEAR THIEF",
            WorldMaps.KameHouse),
        new(
            "OOLONG",
            "O Terror da Vila",
            "Uma pequena comunidade vive com medo de uma criatura capaz de assumir diferentes formas.\n\n"
                + "Descubra quem está por trás dos ataques e enfrente Oolong.",
            "OOLONG",
            WorldMaps.CleanPath),
        new(
            "YAMCHA",
            "O Bandido do Deserto",
            "No caminho pelas terras áridas, Goku encontra um adversário muito mais habilidoso.\n\n"
                + "Yamcha domina o deserto e não pretende deixar os viajantes passarem facilmente.",
            "YAMCHA",
            null),
        new(
            "MONSTER CARROT",
            "A Gangue dos Coelhos",
            "Uma gangue peculiar domina a região e todos parecem temer seu líder.\n\n"
                + "Atravesse seus capangas e enfrente Monster Carrot.",
            "MONSTER CARROT",
            null),
        new(
            "IMPERADOR PILAF",
            "O Castelo de Pilaf",
            "A busca pelas Esferas do Dragão leva Goku ao esconderijo do Imperador Pilaf.\n\n"
                + "Supere seus servos e avance pelo castelo até o confronto final desta jornada.",
            "IMPERADOR PILAF",
            null),
    ];

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
    private Button _lockedActionButton = null!;
    private Button[] _stageButtons = [];
    private Label[] _stageStateLabels = [];
    private Label[] _stageSelectionLabels = [];
    private Label _phaseNumberLabel = null!;
    private Label _phaseBossTitle = null!;
    private Label _phaseSubtitle = null!;
    private Label _phaseDescription = null!;
    private Label _phaseBossName = null!;
    private Label _phaseStatus = null!;
    private Button _debugCommandsToggle = null!;
    private PanelContainer _debugCommandsPanel = null!;
    private Label _debugCommandOutput = null!;
    private LineEdit _debugCommandInput = null!;
    private Player? _boundPlayer;
    private string _portraitCharacterId = string.Empty;
    private int _selectedStageIndex;

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
            "MapModal/MapPanel/MarginContainer/Content/Header/CloseButton"
        );
        _cleanPathButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/Content/DetailPanel/Margin/DetailContent/"
                + "Footer/ActionArea/CleanPathButton"
        );
        _kameHouseButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/Content/DetailPanel/Margin/DetailContent/"
                + "Footer/ActionArea/KameHouseButton"
        );
        _lockedActionButton = GetNode<Button>(
            "MapModal/MapPanel/MarginContainer/Content/DetailPanel/Margin/DetailContent/"
                + "Footer/ActionArea/LockedActionButton"
        );
        string stageTrack =
            "MapModal/MapPanel/MarginContainer/Content/StageScroll/StageTrackCenter/StageTrack";
        _stageButtons =
        [
            GetNode<Button>($"{stageTrack}/Stage01/Marker"),
            GetNode<Button>($"{stageTrack}/Stage02/Marker"),
            GetNode<Button>($"{stageTrack}/Stage03/Marker"),
            GetNode<Button>($"{stageTrack}/Stage04/Marker"),
            GetNode<Button>($"{stageTrack}/Stage05/Marker"),
        ];
        _stageStateLabels =
        [
            GetNode<Label>($"{stageTrack}/Stage01/State"),
            GetNode<Label>($"{stageTrack}/Stage02/State"),
            GetNode<Label>($"{stageTrack}/Stage03/State"),
            GetNode<Label>($"{stageTrack}/Stage04/State"),
            GetNode<Label>($"{stageTrack}/Stage05/State"),
        ];
        _stageSelectionLabels =
        [
            GetNode<Label>($"{stageTrack}/Stage01/Selection"),
            GetNode<Label>($"{stageTrack}/Stage02/Selection"),
            GetNode<Label>($"{stageTrack}/Stage03/Selection"),
            GetNode<Label>($"{stageTrack}/Stage04/Selection"),
            GetNode<Label>($"{stageTrack}/Stage05/Selection"),
        ];
        string detail =
            "MapModal/MapPanel/MarginContainer/Content/DetailPanel/Margin/DetailContent";
        _phaseNumberLabel = GetNode<Label>($"{detail}/PhaseNumber");
        _phaseBossTitle = GetNode<Label>($"{detail}/BossTitle");
        _phaseSubtitle = GetNode<Label>($"{detail}/Subtitle");
        _phaseDescription = GetNode<Label>($"{detail}/Description");
        _phaseBossName = GetNode<Label>($"{detail}/Footer/BossInfo/BossName");
        _phaseStatus = GetNode<Label>($"{detail}/Footer/StatusInfo/Status");
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
        _stageButtons[0].Pressed += SelectStage01;
        _stageButtons[1].Pressed += SelectStage02;
        _stageButtons[2].Pressed += SelectStage03;
        _stageButtons[3].Pressed += SelectStage04;
        _stageButtons[4].Pressed += SelectStage05;
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
        _mapButton.Pressed -= OpenMapModal;
        _closeMapButton.Pressed -= CloseMapModal;
        _cleanPathButton.Pressed -= TravelToCleanPath;
        _kameHouseButton.Pressed -= TravelToKameHouse;
        _stageButtons[0].Pressed -= SelectStage01;
        _stageButtons[1].Pressed -= SelectStage02;
        _stageButtons[2].Pressed -= SelectStage03;
        _stageButtons[3].Pressed -= SelectStage04;
        _stageButtons[4].Pressed -= SelectStage05;
        _debugCommandsToggle.Pressed -= ToggleDebugCommands;
        _debugCommandInput.TextSubmitted -= SubmitDebugCommand;
        UnbindPlayer();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (_profileModal.IsOpen)
            return;

        if (_mapModal.Visible && input is InputEventKey mapKey
            && mapKey.Pressed && !mapKey.Echo && mapKey.Keycode == Key.Escape)
        {
            CloseMapModal();
            GetViewport().SetInputAsHandled();
            return;
        }

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
        _selectedStageIndex = GetCurrentStageIndex();
        RefreshStageSelection();
        _mapModal.Visible = true;
    }

    private void CloseMapModal()
    {
        _mapModal.Visible = false;
    }

    private void SelectStage01() => SelectStage(0);

    private void SelectStage02() => SelectStage(1);

    private void SelectStage03() => SelectStage(2);

    private void SelectStage04() => SelectStage(3);

    private void SelectStage05() => SelectStage(4);

    private void SelectStage(int index)
    {
        if (index < 0 || index >= Stages.Length)
            return;

        _selectedStageIndex = index;
        RefreshStageSelection();
    }

    private void RefreshStageSelection()
    {
        int currentStageIndex = GetCurrentStageIndex();
        for (int index = 0; index < Stages.Length; index++)
        {
            bool isCurrent = index == currentStageIndex && _boundPlayer is not null;
            bool isLocked = Stages[index].MapId is null;
            _stageSelectionLabels[index].Visible = index == _selectedStageIndex;
            _stageStateLabels[index].Text = isLocked
                ? "BLOQUEADA"
                : isCurrent ? "ATUAL" : "DISPONÍVEL";
            _stageStateLabels[index].Modulate = isLocked
                ? new Color("777b85")
                : isCurrent ? new Color("f4c23b") : new Color("b8d7f2");
            _stageButtons[index].Modulate = isLocked
                ? new Color(0.46f, 0.48f, 0.54f)
                : isCurrent ? new Color("f4c23b") : Colors.White;
        }

        StagePresentation selected = Stages[_selectedStageIndex];
        bool selectedIsCurrent = _boundPlayer is not null
            && selected.MapId == _boundPlayer.MapId;
        bool selectedIsLocked = selected.MapId is null;

        _phaseNumberLabel.Text = $"FASE {_selectedStageIndex + 1:00}";
        _phaseBossTitle.Text = selected.Title;
        _phaseSubtitle.Text = selected.Subtitle;
        _phaseDescription.Text = selected.Description;
        _phaseBossName.Text = selected.BossName;
        _phaseStatus.Text = selectedIsLocked
            ? "BLOQUEADA"
            : selectedIsCurrent ? "LOCAL ATUAL" : "DISPONÍVEL";
        _phaseStatus.Modulate = selectedIsLocked
            ? new Color("858892")
            : selectedIsCurrent ? new Color("f4c23b") : new Color("b8d7f2");

        _cleanPathButton.Visible = selected.MapId == WorldMaps.CleanPath;
        _kameHouseButton.Visible = selected.MapId == WorldMaps.KameHouse;
        _lockedActionButton.Visible = selectedIsLocked;

        Button? travelButton = selected.MapId switch
        {
            WorldMaps.CleanPath => _cleanPathButton,
            WorldMaps.KameHouse => _kameHouseButton,
            _ => null,
        };
        if (travelButton is not null)
        {
            travelButton.Disabled = _boundPlayer is null || selectedIsCurrent;
            travelButton.Text = selectedIsCurrent ? "LOCAL ATUAL" : "VIAJAR";
        }
    }

    private int GetCurrentStageIndex()
    {
        if (_boundPlayer?.MapId == WorldMaps.CleanPath)
            return 1;

        return 0;
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
