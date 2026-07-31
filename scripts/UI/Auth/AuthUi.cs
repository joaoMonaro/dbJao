using Godot;

public static class AuthUi
{
    public static Label Title(string text) => new()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        ThemeTypeVariation = "TitleLabel",
    };
    public static LineEdit Input(string placeholder) => new()
    {
        PlaceholderText = placeholder,
        CustomMinimumSize = new Vector2(0, 44),
    };
    public static Button PrimaryButton(string text) => new()
    {
        Text = text,
        CustomMinimumSize = new Vector2(0, 46),
    };
    public static Button LinkButton(string text) => new()
    {
        Text = text,
        Flat = true,
    };
    public static Label Status() => new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        HorizontalAlignment = HorizontalAlignment.Center,
        CustomMinimumSize = new Vector2(0, 36),
    };
}
