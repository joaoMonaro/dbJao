using Godot;
using System;

public partial class DebugXpCommandsIntegrationTest : NetworkManager
{
    private Player _player = null!;

    public override void _Ready()
    {
        Node2D players = new() { Name = "Players" };
        AddChild(players);

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/Player.tscn");
        _player = playerScene.Instantiate<Player>();
        _player.Name = "2";
        _player.OwnerPeerId = 2;
        _player.CharacterId = "debug-character";
        players.AddChild(_player);

        CallDeferred(MethodName.RunTests);
    }

    private void RunTests()
    {
        try
        {
            Assert(RunningAsServer && Multiplayer.IsServer(),
                "O teste precisa executar como servidor.");
            Assert(_player.BaseBattlePower == 10,
                "Personagem novo não iniciou com Poder de Luta 10.");
            Assert(_player.ActiveCharacterId == "goku"
                && _player.ActiveCharacterDefinition.Name == "Goku",
                "Personagem novo não iniciou com Goku ativo.");
            Assert(_player.CurrentCombatStats == new CombatStats(10, 10, 10),
                "Stats iniciais do Goku não foram derivados do Poder de Luta.");

            DebugXpCommandResult blocked = Execute("/addxp 100", enabled: false);
            Assert(!blocked.Success && _player.TotalXp == 0,
                "Comando alterou XP com debug desabilitado.");

            AssertRejected("/addxp", "Quantidade ausente foi aceita.");
            AssertRejected("/addxp texto", "Quantidade não numérica foi aceita.");
            AssertRejected("/addxp -1", "Quantidade negativa foi aceita.");
            AssertRejected("/addxp 9223372036854775808", "Overflow textual foi aceito.");

            DebugXpCommandResult insufficientXp = Execute("/addxp 25");
            Assert(insufficientXp.Success && _player.BaseBattlePower == 10,
                "XP insuficiente alterou o Poder de Luta.");

            SetProgression(0);
            DebugXpCommandResult addXp = Execute("/addxp 100");
            Assert(addXp.Success && addXp.GrantedXp == 100,
                "/addxp 100 não foi executado.");
            Assert(_player.TotalXp == 100 && _player.Level == 1 && _player.Reset == 0,
                "/addxp não passou pela progressão real.");
            Assert(_player.BaseBattlePower == 110,
                "Um Level Up não concedeu exatamente 100 de Poder de Luta.");
            Assert(_player.CurrentCombatStats == new CombatStats(110, 110, 110),
                "Stats derivados não acompanharam o Poder de Luta.");

            SetProgression(125);
            ProgressionSnapshot beforeNext = Player.RebuildProgression(_player.TotalXp);
            long battlePowerBeforeNext = _player.BaseBattlePower;
            long expectedMissing = beforeNext.XpRequiredForNextLevel - beforeNext.XpIntoLevel;
            DebugXpCommandResult addNext = Execute("/addxpnext");
            Assert(addNext.Success && addNext.GrantedXp == expectedMissing,
                "/addxpnext não concedeu exatamente o XP faltante.");
            Assert(_player.Level == beforeNext.State.Level + 1
                && _player.CurrentExperience == 0,
                "/addxpnext não completou exatamente o Level atual.");
            Assert(_player.BaseBattlePower == battlePowerBeforeNext + 100,
                "/addxpnext não concedeu Poder de Luta pelo Level concluído.");

            long level199Start = TotalXpAtGlobalLevel(199);
            SetProgression(level199Start);
            DebugXpCommandResult reset = Execute("/addxpnext");
            Assert(reset.Success && _player.Level == 0 && _player.Reset == 1,
                "/addxpnext no Level 199 não completou o Reset.");
            Assert(_player.BaseBattlePower == 20010,
                "Level 199 -> Reset concedeu valor incorreto de Poder de Luta.");

            ProgressionState stateBeforeInfo =
                new(_player.TotalXp, _player.Level, _player.Reset);
            long battlePowerBeforeInfo = _player.BaseBattlePower;
            DebugXpCommandResult info = Execute("/xpinfo");
            Assert(info.Success && info.Message.Contains("GlobalLevel: 200", StringComparison.Ordinal),
                "/xpinfo não exibiu o GlobalLevel esperado.");
            Assert(info.Message.Contains("Poder de Luta base: 20010", StringComparison.Ordinal),
                "/xpinfo não exibiu o Poder de Luta esperado.");
            Assert(stateBeforeInfo == new ProgressionState(
                    _player.TotalXp, _player.Level, _player.Reset)
                && _player.BaseBattlePower == battlePowerBeforeInfo,
                "/xpinfo alterou o estado da progressão.");

            SetProgression(0);
            long throughMultipleResets = TotalXpAtGlobalLevel(420);
            DebugXpCommandResult multipleResets = Execute($"/addxp {throughMultipleResets}");
            Assert(multipleResets.Success && _player.Reset == 2 && _player.Level == 20,
                "Uma concessão grande não atravessou múltiplos Resets.");
            Assert(_player.BaseBattlePower == 42010,
                "Múltiplos Levels/Resets não concederam um incremento por Level.");

            long reconnectXp = TotalXpAtGlobalLevel(5);
            AuthenticatedCharacterData persisted = new()
            {
                UserId = Guid.NewGuid(),
                CharacterId = Guid.NewGuid(),
                CharacterName = "Reconnect Test",
                TotalXp = reconnectXp,
                Level = 5,
                Reset = 0,
                BaseBattlePower = 510,
                ActiveCharacterId = "goku",
                CurrentHealth = 100,
                MaxHealth = 100,
                MapId = WorldMaps.KameHouse,
            };
            _player.ApplyAuthenticatedInitialState(persisted);
            _player.ApplyAuthenticatedInitialState(persisted);
            Assert(_player.BaseBattlePower == 510,
                "Reconexão concedeu Poder de Luta novamente.");
            Assert(_player.ActiveCharacterId == "goku"
                && _player.CurrentCombatStats == new CombatStats(510, 510, 510),
                "Reconexão não preservou o personagem ativo e seus stats.");
            Assert(_player.TryCaptureAuthenticatedState(out AuthenticatedPlayerState? saved)
                && saved?.BaseBattlePower == 510
                && saved.ActiveCharacterId == "goku",
                "Snapshot persistente não preservou Poder de Luta/personagem ativo.");

            SetProgression(0);
            _player.BaseBattlePower = long.MaxValue;
            DebugXpCommandResult overflow = Execute("/addxp 100");
            Assert(!overflow.Success && _player.TotalXp == 0 && _player.Level == 0
                && _player.Reset == 0 && _player.BaseBattlePower == long.MaxValue,
                "Overflow de Poder de Luta não preservou o estado anterior.");

            GD.Print("[PASS] Comandos de debug de XP validados.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[DEBUG XP TEST] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private DebugXpCommandResult Execute(string command, bool enabled = true) =>
        DebugXpCommands.Execute(_player, command, enabled);

    private void AssertRejected(string command, string message)
    {
        long totalXp = _player.TotalXp;
        DebugXpCommandResult result = Execute(command);
        Assert(!result.Success && _player.TotalXp == totalXp, message);
    }

    private void SetProgression(long totalXp)
    {
        ProgressionSnapshot snapshot = Player.RebuildProgression(totalXp);
        _player.TotalXp = snapshot.State.TotalXp;
        _player.Level = snapshot.State.Level;
        _player.Reset = snapshot.State.Reset;
        long completedLevels = Player.GetGlobalLevel(snapshot.State.Reset, snapshot.State.Level);
        _player.BaseBattlePower = new BattlePowerProgression(BattlePowerSettings.Default)
            .FromCompletedLevels(completedLevels);
    }

    private static long TotalXpAtGlobalLevel(int targetGlobalLevel)
    {
        ExponentialXpCurve curve = new(XpCurveSettings.Default);
        long total = 0;
        for (int globalLevel = 0; globalLevel < targetGlobalLevel; globalLevel++)
        {
            total = checked(total + curve.GetRequiredXpForNextLevel(
                globalLevel / curve.Settings.LevelsPerReset,
                globalLevel % curve.Settings.LevelsPerReset));
        }

        return total;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
