using Godot;

public partial class Hud : CanvasLayer
{
    [Export] public StringName PlayerGroup { get; set; } = new("player");

    private ProgressBar _healthBar = null!;
    private ProgressBar _manaBar = null!;
    private ProgressBar _experienceBar = null!;
    private Label _healthLabel = null!;
    private TextureButton _mapButton = null!;
    private Control _mapModal = null!;
    private Button _closeMapButton = null!;
    private Button _cleanPathButton = null!;

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

        _mapButton.Pressed += OpenMapModal;
        _closeMapButton.Pressed += CloseMapModal;
        _cleanPathButton.Pressed += TravelToCleanPath;

        Player? player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Player;
        if (!GodotObject.IsInstanceValid(player))
            return;

        player!.HealthChanged += OnHealthChanged;
        player.ManaChanged += OnManaChanged;
        player.ExperienceChanged += OnExperienceChanged;

        OnHealthChanged(player.CurrentHealth, player.MaxHealth);
        OnManaChanged(player.CurrentMana, player.MaxMana);
        OnExperienceChanged(player.CurrentExperience, player.MaxExperience);
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

    private void OnExperienceChanged(int current, int maximum)
    {
        _experienceBar.MaxValue = Mathf.Max(maximum, 1);
        _experienceBar.Value = current;
    }

    private void OpenMapModal()
    {
        _mapModal.Visible = true;
    }

    private void CloseMapModal()
    {
        _mapModal.Visible = false;
    }

    private void TravelToCleanPath()
    {
        GetTree().ChangeSceneToFile("res://scenes/CleanPath.tscn");
    }
}
