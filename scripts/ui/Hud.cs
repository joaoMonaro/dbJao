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

        GetTree().NodeAdded += OnNodeAdded;
        CallDeferred(MethodName.TryBindLocalPlayer);
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= OnNodeAdded;
        UnbindPlayer();
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
            player.TreeExiting += OnBoundPlayerExiting;

            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
            OnManaChanged(player.CurrentMana, player.MaxMana);
            OnExperienceChanged(player.CurrentExperience, player.MaxExperience);
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
