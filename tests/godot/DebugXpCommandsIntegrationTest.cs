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

            DebugXpCommandResult blocked = Execute("/addxp 100", enabled: false);
            Assert(!blocked.Success && _player.TotalXp == 0,
                "Comando alterou XP com debug desabilitado.");

            AssertRejected("/addxp", "Quantidade ausente foi aceita.");
            AssertRejected("/addxp texto", "Quantidade não numérica foi aceita.");
            AssertRejected("/addxp -1", "Quantidade negativa foi aceita.");
            AssertRejected("/addxp 9223372036854775808", "Overflow textual foi aceito.");

            DebugXpCommandResult addXp = Execute("/addxp 100");
            Assert(addXp.Success && addXp.GrantedXp == 100,
                "/addxp 100 não foi executado.");
            Assert(_player.TotalXp == 100 && _player.Level == 1 && _player.Reset == 0,
                "/addxp não passou pela progressão real.");

            SetProgression(125);
            ProgressionSnapshot beforeNext = Player.RebuildProgression(_player.TotalXp);
            long expectedMissing = beforeNext.XpRequiredForNextLevel - beforeNext.XpIntoLevel;
            DebugXpCommandResult addNext = Execute("/addxpnext");
            Assert(addNext.Success && addNext.GrantedXp == expectedMissing,
                "/addxpnext não concedeu exatamente o XP faltante.");
            Assert(_player.Level == beforeNext.State.Level + 1
                && _player.CurrentExperience == 0,
                "/addxpnext não completou exatamente o Level atual.");

            long level199Start = TotalXpAtGlobalLevel(199);
            SetProgression(level199Start);
            DebugXpCommandResult reset = Execute("/addxpnext");
            Assert(reset.Success && _player.Level == 0 && _player.Reset == 1,
                "/addxpnext no Level 199 não completou o Reset.");

            ProgressionState stateBeforeInfo =
                new(_player.TotalXp, _player.Level, _player.Reset);
            DebugXpCommandResult info = Execute("/xpinfo");
            Assert(info.Success && info.Message.Contains("GlobalLevel: 200", StringComparison.Ordinal),
                "/xpinfo não exibiu o GlobalLevel esperado.");
            Assert(stateBeforeInfo == new ProgressionState(
                    _player.TotalXp, _player.Level, _player.Reset),
                "/xpinfo alterou o estado da progressão.");

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
