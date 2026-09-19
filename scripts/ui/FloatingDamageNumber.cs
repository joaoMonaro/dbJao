using Godot;
using System;

public partial class FloatingDamageNumber : Node2D
{
    public static readonly Color DealtDamageColor = Colors.White;
    public static readonly Color ReceivedDamageColor = new("ff4a4a");

    [Export] public float RiseDistance { get; set; } = 42.0f;
    [Export] public float LifetimeSeconds { get; set; } = 0.85f;
    [Export] public float FadeDelaySeconds { get; set; } = 0.25f;

    public Label DamageLabel { get; private set; } = null!;

    public override void _Ready()
    {
        DamageLabel = GetNode<Label>("DamageLabel");
    }

    public void ShowDamage(long amount, bool isReceivedDamage)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (!float.IsFinite(RiseDistance) || RiseDistance <= 0.0f)
            throw new InvalidOperationException("A distância do número de dano deve ser positiva.");

        if (!float.IsFinite(LifetimeSeconds) || LifetimeSeconds <= 0.0f)
            throw new InvalidOperationException("A duração do número de dano deve ser positiva.");

        DamageLabel.Text = amount.ToString();
        DamageLabel.Modulate = isReceivedDamage ? ReceivedDamageColor : DealtDamageColor;

        Tween movement = CreateTween();
        movement.SetTrans(Tween.TransitionType.Quad);
        movement.SetEase(Tween.EaseType.Out);
        movement.TweenProperty(this, "position:y", Position.Y - RiseDistance, LifetimeSeconds);
        movement.TweenCallback(Callable.From(QueueFree));

        float fadeDelay = Mathf.Clamp(FadeDelaySeconds, 0.0f, LifetimeSeconds);
        Tween fade = CreateTween();
        fade.TweenInterval(fadeDelay);
        fade.TweenProperty(
            this,
            "modulate:a",
            0.0f,
            Mathf.Max(LifetimeSeconds - fadeDelay, 0.01f));
    }
}
