using Godot;
using System;

public partial class PlayerProfileModal : Control
{
    private Button _closeButton = null!;
    private Label _characterName = null!;
    private TextureRect _portrait = null!;
    private Label _battlePowerValue = null!;
    private Label _levelValue = null!;
    private Label _resetValue = null!;
    private Label _attackValue = null!;
    private Label _defenseValue = null!;
    private Label _kiAttackValue = null!;
    private Label _maxHealthValue = null!;
    private Label _attackMultiplier = null!;
    private Label _defenseMultiplier = null!;
    private Label _kiAttackMultiplier = null!;
    private Label _maxHealthMultiplier = null!;
    private ProgressBar _experienceBar = null!;
    private Label _experienceLabel = null!;
    private Player? _player;
    private string _portraitCharacterId = string.Empty;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        _closeButton = GetNode<Button>(
            "ProfilePanel/MarginContainer/Content/Header/CloseButton");
        _characterName = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Header/Identity/CharacterName");
        _portrait = GetNode<TextureRect>(
            "ProfilePanel/MarginContainer/Content/Overview/PortraitFrame/Portrait");
        _battlePowerValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Overview/Progression/BattlePowerValue");
        _levelValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Overview/Progression/LevelReset/LevelValue");
        _resetValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Overview/Progression/LevelReset/ResetValue");
        _attackValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/AttackValue");
        _defenseValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/DefenseValue");
        _kiAttackValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/KiAttackValue");
        _maxHealthValue = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/MaxHealthValue");
        _attackMultiplier = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/AttackMultiplier");
        _defenseMultiplier = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/DefenseMultiplier");
        _kiAttackMultiplier = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/KiAttackMultiplier");
        _maxHealthMultiplier = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/Attributes/MaxHealthMultiplier");
        _experienceBar = GetNode<ProgressBar>(
            "ProfilePanel/MarginContainer/Content/ExperienceBar");
        _experienceLabel = GetNode<Label>(
            "ProfilePanel/MarginContainer/Content/ExperienceBar/Label");

        _closeButton.Pressed += Close;
        Visible = false;
    }

    public override void _ExitTree()
    {
        _closeButton.Pressed -= Close;
        UnbindPlayer();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (!Visible || input is not InputEventKey key
            || !key.Pressed || key.Echo || key.Keycode != Key.Escape)
        {
            return;
        }

        Close();
        GetViewport().SetInputAsHandled();
    }

    public void Bind(Player? player)
    {
        if (_player == player)
            return;

        UnbindPlayer();
        _player = player;
        if (!GodotObject.IsInstanceValid(_player))
            return;

        _player!.HealthChanged += OnHealthChanged;
        _player.ExperienceChanged += OnExperienceChanged;
        _player.ProgressionChanged += OnProgressionChanged;
        _player.BattlePowerChanged += OnBattlePowerChanged;
        _player.CombatStatsChanged += OnCombatStatsChanged;
        _player.TreeExiting += OnPlayerExiting;
        RefreshAll();
    }

    public void Open()
    {
        if (!GodotObject.IsInstanceValid(_player))
            return;

        RefreshAll();
        Visible = true;
        _closeButton.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        _closeButton.ReleaseFocus();
    }

    private void RefreshAll()
    {
        if (!GodotObject.IsInstanceValid(_player))
            return;

        Player player = _player!;
        OnHealthChanged(player.CurrentHealth, player.MaxHealth);
        OnExperienceChanged(player.CurrentExperience, player.MaxExperience);
        OnProgressionChanged(player.Level, player.Reset);
        OnBattlePowerChanged(player.BaseBattlePower);
        CombatStats stats = player.CurrentCombatStats;
        OnCombatStatsChanged(
            player.ActiveCharacterDefinition.Name,
            stats.Attack,
            stats.Defense,
            stats.KiAttack);
    }

    private void OnHealthChanged(int current, int maximum)
    {
        _maxHealthValue.Text = maximum.ToString("N0");
    }

    private void OnExperienceChanged(long current, long maximum)
    {
        _experienceBar.MaxValue = maximum > 0 ? maximum : 1;
        _experienceBar.Value = current;
        _experienceLabel.Text = $"{current:N0} / {maximum:N0} XP";
    }

    private void OnProgressionChanged(int level, long reset)
    {
        _levelValue.Text = $"LEVEL {level}";
        _resetValue.Text = $"RESET {reset}";
    }

    private void OnBattlePowerChanged(long baseBattlePower)
    {
        _battlePowerValue.Text = baseBattlePower.ToString("N0");
    }

    private void OnCombatStatsChanged(
        string characterName,
        long attack,
        long defense,
        long kiAttack)
    {
        _characterName.Text = characterName.ToUpperInvariant();
        _attackValue.Text = attack.ToString("N0");
        _defenseValue.Text = defense.ToString("N0");
        _kiAttackValue.Text = kiAttack.ToString("N0");

        if (!GodotObject.IsInstanceValid(_player))
            return;

        CharacterDefinition definition = _player!.ActiveCharacterDefinition;
        _attackMultiplier.Text = FormatMultiplier(definition.AttackMultiplier);
        _defenseMultiplier.Text = FormatMultiplier(definition.DefenseMultiplier);
        _kiAttackMultiplier.Text = FormatMultiplier(definition.KiAttackMultiplier);
        _maxHealthMultiplier.Text = FormatMultiplier(definition.MaxHealthMultiplier);
        LoadPortrait(definition);
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

    private void OnPlayerExiting()
    {
        Close();
        UnbindPlayer();
    }

    private void UnbindPlayer()
    {
        if (!GodotObject.IsInstanceValid(_player))
        {
            _portrait.Texture = null;
            _portraitCharacterId = string.Empty;
            _player = null;
            return;
        }

        _player!.HealthChanged -= OnHealthChanged;
        _player.ExperienceChanged -= OnExperienceChanged;
        _player.ProgressionChanged -= OnProgressionChanged;
        _player.BattlePowerChanged -= OnBattlePowerChanged;
        _player.CombatStatsChanged -= OnCombatStatsChanged;
        _player.TreeExiting -= OnPlayerExiting;
        _portrait.Texture = null;
        _portraitCharacterId = string.Empty;
        _player = null;
    }

    private static string FormatMultiplier(decimal multiplier) =>
        $"{multiplier * 100m:0.##}%";
}
