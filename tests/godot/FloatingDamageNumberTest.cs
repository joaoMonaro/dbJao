using Godot;
using System;

public partial class FloatingDamageNumberTest : Node2D
{
    public override void _Ready()
    {
        CallDeferred(MethodName.RunTests);
    }

    private async void RunTests()
    {
        try
        {
            PackedScene playerScene = GD.Load<PackedScene>("res://scenes/Player.tscn");
            Player player = playerScene.Instantiate<Player>();
            player.Name = "2";
            player.OwnerPeerId = 2;
            AddChild(player);

            Assert(player.FloatingDamageNumberScene is not null,
                "Player não possui a cena de número de dano configurada.");

            Vector2 dealtOrigin = new(180.0f, 220.0f);
            Assert(player.ShowLocalDamageNumber(321, dealtOrigin, isReceivedDamage: false),
                "Não foi possível criar o número de dano causado.");
            FloatingDamageNumber dealt = FindNewestDamageNumber();
            Assert(dealt.DamageLabel.Text == "321"
                && dealt.DamageLabel.Modulate == FloatingDamageNumber.DealtDamageColor,
                "Dano causado não foi exibido em branco com o valor correto.");

            Vector2 receivedOrigin = new(260.0f, 220.0f);
            Assert(player.ShowLocalDamageNumber(17, receivedOrigin, isReceivedDamage: true),
                "Não foi possível criar o número de dano recebido.");
            FloatingDamageNumber received = FindNewestDamageNumber(excluding: dealt);
            Assert(received.DamageLabel.Text == "17"
                && received.DamageLabel.Modulate == FloatingDamageNumber.ReceivedDamageColor,
                "Dano recebido não foi exibido em vermelho com o valor correto.");

            await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
            Assert(dealt.Position.Y < dealtOrigin.Y && received.Position.Y < receivedOrigin.Y,
                "Os números de dano não subiram após serem exibidos.");

            await ToSignal(GetTree().CreateTimer(0.35f), SceneTreeTimer.SignalName.Timeout);
            Assert(dealt.Modulate.A < 1.0f && received.Modulate.A < 1.0f,
                "Os números de dano não iniciaram o fade esperado.");

            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
            Assert(!GodotObject.IsInstanceValid(dealt) && !GodotObject.IsInstanceValid(received),
                "Os números de dano não desapareceram após a animação.");

            GD.Print("[PASS] Feedback visual de dano validado.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[DAMAGE NUMBER TEST] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private FloatingDamageNumber FindNewestDamageNumber(FloatingDamageNumber? excluding = null)
    {
        foreach (Node child in GetChildren())
        {
            if (child is FloatingDamageNumber number && number != excluding)
                return number;
        }

        throw new InvalidOperationException("Número de dano não foi adicionado à cena atual.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
