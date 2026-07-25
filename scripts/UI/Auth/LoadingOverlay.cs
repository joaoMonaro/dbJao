using Godot;

public partial class LoadingOverlay : ColorRect
{
    private Label _message = null!;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Color = new Color(0, 0, 0, .68f);
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _message = new Label { Text = "Carregando..." };
        center.AddChild(_message);
        AddChild(center);
        Hide();
    }
    public void ShowMessage(string message) { _message.Text = message; Show(); }
}
