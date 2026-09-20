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

            ValidateStageSelector();

            GD.Print("[PASS] HUD, perfil e seletor de fases validados.");
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

    private void ValidateStageSelector()
    {
        const string modalRoot = "MapModal/MapPanel/MarginContainer/Content";
        const string track = $"{modalRoot}/StageScroll/StageTrackCenter/StageTrack";
        const string detail = $"{modalRoot}/DetailPanel/Margin/DetailContent";

        TextureButton phaseButton = _hud.GetNode<TextureButton>("MapButton");
        Control phaseModal = _hud.GetNode<Control>("MapModal");
        Assert(phaseButton.TooltipText == "Fases" && !phaseModal.Visible,
            "Acesso lateral de fases está inconsistente.");

        phaseButton.EmitSignal(TextureButton.SignalName.Pressed);
        Assert(phaseModal.Visible && !GetTree().Paused,
            "Seletor de fases não abriu como camada de UI multiplayer.");
        Assert(_hud.GetNode<Label>($"{modalRoot}/Header/Titles/Title").Text == "FASES"
            && _hud.GetNode<Label>($"{modalRoot}/Header/Titles/Saga").Text
                == "A BUSCA PELAS ESFERAS",
            "Cabeçalho narrativo do seletor está incorreto.");

        (string NodeName, string DisplayName)[] expectedStages =
        {
            ("Spawn", "SPAWN"),
            ("Stage01", "BEAR THIEF"),
            ("Stage02", "OOLONG"),
            ("Stage03", "YAMCHA"),
            ("Stage04", "MONSTER CARROT"),
            ("Stage05", "IMPERADOR PILAF"),
        };
        foreach ((string nodeName, string displayName) in expectedStages)
        {
            Assert(_hud.GetNode<Label>($"{track}/{nodeName}/Name").Text == displayName,
                $"Destino {nodeName} não apresentou o nome esperado.");
        }

        Assert(_hud.GetNode<Label>($"{track}/Spawn/State").Text == "ATUAL"
            && _hud.GetNode<Label>($"{track}/Spawn/Selection").Visible
            && _hud.GetNodeOrNull<Control>($"{track}/SpawnGap") is not null
            && _hud.GetNodeOrNull<ColorRect>($"{track}/SpawnGap/Line") is null,
            "Spawn isolado não recebeu estado, seleção ou separação corretos.");
        Assert(_hud.GetNode<Label>($"{detail}/PhaseNumber").Text == "PONTO DE PARTIDA"
            && _hud.GetNode<Label>($"{detail}/BossTitle").Text == "KAME HOUSE"
            && _hud.GetNode<Label>($"{detail}/Footer/BossInfo/Label").Text == "LOCAL INICIAL"
            && _hud.GetNode<Label>($"{detail}/Footer/BossInfo/BossName").Text == "KAME HOUSE",
            "Ficha do ponto de partida não apresentou a Kame House.");
        Button kameHouseAction = _hud.GetNode<Button>(
            $"{detail}/Footer/ActionArea/KameHouseButton");
        Assert(kameHouseAction.Visible && kameHouseAction.Disabled
            && kameHouseAction.Text == "LOCAL ATUAL",
            "Ação do spawn não preservou o bloqueio de viagem para o mapa atual.");

        _hud.GetNode<Button>($"{track}/Stage01/Marker").EmitSignal(Button.SignalName.Pressed);
        Assert(_hud.GetNode<Label>($"{detail}/PhaseNumber").Text == "FASE 01"
            && _hud.GetNode<Label>($"{detail}/BossTitle").Text == "BEAR THIEF"
            && _hud.GetNode<Label>($"{detail}/Subtitle").Text == "O Ladrão da Estrada"
            && _hud.GetNode<Label>($"{detail}/Description").Text.Contains(
                "bloqueia o caminho", StringComparison.Ordinal)
            && _hud.GetNode<Label>($"{detail}/Footer/BossInfo/BossName").Text == "BEAR THIEF",
            "Painel narrativo não atualizou para Bear Thief.");
        Button cleanPathAction = _hud.GetNode<Button>(
            $"{detail}/Footer/ActionArea/CleanPathButton");
        Assert(cleanPathAction.Visible && !cleanPathAction.Disabled
            && cleanPathAction.Text == "VIAJAR"
            && _hud.GetNode<Label>($"{track}/Stage01/State").Text == "DISPONÍVEL"
            && _player.MapId == WorldMaps.KameHouse,
            "Selecionar Bear Thief viajou imediatamente ou alterou a ação existente.");

        _hud.GetNode<Button>($"{track}/Stage02/Marker").EmitSignal(Button.SignalName.Pressed);
        Button lockedAction = _hud.GetNode<Button>(
            $"{detail}/Footer/ActionArea/LockedActionButton");
        Assert(_hud.GetNode<Label>($"{detail}/BossTitle").Text == "OOLONG"
            && _hud.GetNode<Label>($"{detail}/Subtitle").Text == "O Terror da Vila"
            && _hud.GetNode<Label>($"{track}/Stage02/State").Text == "BLOQUEADA"
            && _hud.GetNode<Label>($"{detail}/Footer/StatusInfo/Status").Text == "BLOQUEADA"
            && lockedAction.Visible && lockedAction.Disabled
            && _player.MapId == WorldMaps.KameHouse,
            "Fase futura não preservou seu estado apenas visual e bloqueado.");

        _hud.GetNode<Button>($"{track}/Stage05/Marker").EmitSignal(Button.SignalName.Pressed);
        Assert(_hud.GetNode<Label>($"{track}/Stage05/FinalBadge").Text == "FINAL"
            && _hud.GetNode<Label>($"{detail}/BossTitle").Text == "IMPERADOR PILAF"
            && _hud.GetNode<Label>($"{detail}/Subtitle").Text == "O Castelo de Pilaf",
            "Final da saga não recebeu a apresentação do Imperador Pilaf.");

        _hud.GetNode<Button>($"{modalRoot}/Header/CloseButton")
            .EmitSignal(Button.SignalName.Pressed);
        Assert(!phaseModal.Visible, "Botão X não fechou o seletor de fases.");

        phaseButton.EmitSignal(TextureButton.SignalName.Pressed);
        _hud._UnhandledInput(new InputEventKey
        {
            Keycode = Key.Escape,
            Pressed = true,
        });
        Assert(!phaseModal.Visible, "Tecla Esc não fechou o seletor de fases.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
