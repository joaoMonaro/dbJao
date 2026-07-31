using Godot;

public partial class PasswordField : HBoxContainer
{
    public LineEdit Input { get; private set; } = null!;
    public string Text
    {
        get => Input.Text;
        set => Input.Text = value;
    }

    public override void _Ready()
    {
        Input = new LineEdit
        {
            Secret = true,
            PlaceholderText = "Senha",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        Button toggle = new() { Text = "Mostrar", CustomMinimumSize = new Vector2(90, 0) };
        toggle.Pressed += () =>
        {
            Input.Secret = !Input.Secret;
            toggle.Text = Input.Secret ? "Mostrar" : "Ocultar";
            Input.GrabFocus();
        };
        AddChild(Input);
        AddChild(toggle);
    }

    public void SetEditable(bool editable) => Input.Editable = editable;
}
