using Godot;
using System;

public partial class HudProfileIntegrationTest : Node
{
    private CanvasLayer _hud = null!;
    private Player _player = null!;

    public override void _Ready()
    {
        Node2D players = new() { Name = "Players" };
        AddChild(players);

        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        _player.Name = "1";
        _player.OwnerPeerId = 1;
        _player.CharacterId = "hud-test-character";
        players.AddChild(_player);

        _hud = GD.Load<PackedScene>("res://scenes/ui/HUD.tscn").Instantiate<CanvasLayer>();
        AddChild(_hud);

        CallDeferred(MethodName.RunTests);
    }

    private void RunTests()
    {
        try
        {
            const string hudRoot =
                "PlayerHudMargin/PlayerHudPanel/InnerMargin/Content";
            Button portraitButton = _hud.GetNode<Button>($"{hudRoot}/PortraitButton");
            TextureRect portrait = _hud.GetNode<TextureRect>(
                $"{hudRoot}/PortraitButton/Portrait");
            Label healthLabel = _hud.GetNode<Label>($"{hudRoot}/Bars/HealthBar/Label");
            Label kiLabel = _hud.GetNode<Label>($"{hudRoot}/Bars/ManaBar/Label");
            Label xpLabel = _hud.GetNode<Label>($"{hudRoot}/Bars/ExperienceBar/Label");
            PlayerProfileModal modal = _hud.GetNode<PlayerProfileModal>("PlayerProfileModal");

            Assert(portrait.Texture is not null,
                "HUD não carregou o portrait do personagem ativo.");
            Assert(healthLabel.Text == "HP 100 / 100"
                && kiLabel.Text == "KI 100 / 100"
                && xpLabel.Text.Contains("XP", StringComparison.Ordinal),
                "HUD compacto não mostrou somente as três barras essenciais.");
            Assert(_hud.GetNodeOrNull<Label>($"{hudRoot}/Bars/ProgressionLabel") is null
                && _hud.GetNodeOrNull<Label>($"{hudRoot}/Bars/BattlePowerLabel") is null
                && _hud.GetNodeOrNull<Label>($"{hudRoot}/Bars/CombatStatsLabel") is null,
                "Dados detalhados continuam expostos no HUD permanente.");
            Assert(!modal.Visible, "Modal de perfil abriu automaticamente.");

            portraitButton.EmitSignal(Button.SignalName.Pressed);
            Assert(modal.Visible, "Clique no portrait não abriu o modal.");
            Assert(ProfileLabel("Header/Identity/CharacterName").Text == "GOKU",
                "Modal não resolveu o nome pela definição ativa.");
            Assert(ProfileLabel("Attributes/AttackMultiplier").Text == "100%"
                && ProfileLabel("Attributes/DefenseMultiplier").Text == "100%"
                && ProfileLabel("Attributes/KiAttackMultiplier").Text == "100%"
                && ProfileLabel("Attributes/MaxHealthMultiplier").Text == "100%",
                "Modal não exibiu os multiplicadores da definição.");

            _player.TotalXp = 100;
            _player.Level = 1;
            _player.Reset = 0;
            _player.BaseBattlePower = 110;
            _player.Health.CurrentHealth = 55;

            Assert(healthLabel.Text == "HP 55 / 100",
                "HUD de HP não reagiu ao sinal de vida.");
            Assert(ProfileLabel("Overview/Progression/BattlePowerValue").Text.Contains(
                    "110", StringComparison.Ordinal)
                && ProfileLabel("Overview/Progression/LevelReset/LevelValue").Text == "LEVEL 1"
                && ProfileLabel("Attributes/AttackValue").Text.Contains(
                    "110", StringComparison.Ordinal)
                && ProfileLabel("Attributes/DefenseValue").Text.Contains(
                    "110", StringComparison.Ordinal)
                && ProfileLabel("Attributes/KiAttackValue").Text.Contains(
                    "110", StringComparison.Ordinal)
                && ProfileLabel("Attributes/MaxHealthValue").Text == "100",
                "Modal aberto não atualizou progressão e atributos em tempo real.");
            Assert(!GetTree().Paused, "Abrir o perfil pausou a árvore multiplayer.");

            Button closeButton = _hud.GetNode<Button>(
                "PlayerProfileModal/ProfilePanel/MarginContainer/Content/Header/CloseButton");
            closeButton.EmitSignal(Button.SignalName.Pressed);
            Assert(!modal.Visible, "Botão X não fechou o modal.");

            portraitButton.EmitSignal(Button.SignalName.Pressed);
            modal._UnhandledInput(new InputEventKey
            {
                Keycode = Key.Escape,
                Pressed = true,
            });
            Assert(!modal.Visible, "Tecla Esc não fechou o modal.");

            GD.Print("[PASS] HUD compacto e modal de perfil validados.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[HUD PROFILE TEST] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private Label ProfileLabel(string relativePath) =>
        _hud.GetNode<Label>(
            $"PlayerProfileModal/ProfilePanel/MarginContainer/Content/{relativePath}");

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
