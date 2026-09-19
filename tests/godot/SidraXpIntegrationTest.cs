using Godot;
using System;

public partial class SidraXpIntegrationTest : NetworkManager
{
    private Player _firstPlayer = null!;
    private Player _secondPlayer = null!;
    private Sidra _sidra = null!;

    public override void _Ready()
    {
        Node2D players = new() { Name = "Players" };
        Node2D npcs = new() { Name = "NPCs" };
        AddChild(players);
        AddChild(npcs);

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/Player.tscn");
        _firstPlayer = CreatePlayer(playerScene, players, 2, "character-a");
        _secondPlayer = CreatePlayer(playerScene, players, 3, "character-b");

        _sidra = GD.Load<PackedScene>("res://scenes/Sidra.tscn").Instantiate<Sidra>();
        _sidra.Name = "Sidra";
        npcs.AddChild(_sidra);

        CallDeferred(MethodName.RunTests);
    }

    private void RunTests()
    {
        try
        {
            Assert(RunningAsServer && Multiplayer.IsServer(),
                "O teste precisa executar como servidor.");
            DamageInfo firstHit = PlayerDamage(_firstPlayer, _sidra.MaxHealth - 1);
            Assert(_sidra.ApplyServerDamage(firstHit), "Dano não fatal deveria ser aceito.");
            Assert(!_sidra.IsDead, "Sidra morreu antes do golpe fatal.");
            Assert(_firstPlayer.TotalXp == 0 && _secondPlayer.TotalXp == 0,
                "Sidra concedeu XP enquanto ainda estava vivo.");

            DamageInfo killingBlow = PlayerDamage(_secondPlayer, 1);
            Assert(_sidra.ApplyServerDamage(killingBlow), "Golpe fatal deveria ser aceito.");
            Assert(_sidra.IsDead, "Servidor não confirmou a morte do Sidra.");
            Assert(_firstPlayer.TotalXp == 0, "XP foi concedido ao jogador incorreto.");
            long grantedXp = _secondPlayer.TotalXp;
            Assert(grantedXp > 0, "Golpe fatal não concedeu XP.");
            Assert(!_sidra.ApplyServerDamage(killingBlow),
                "Dano repetido em Sidra morto deveria ser rejeitado.");
            Assert(_secondPlayer.TotalXp == grantedXp,
                "Uma morte concedeu XP mais de uma vez.");

            RespawnSidra();
            long firstLevelRequired = Player.RebuildProgression(0).XpRequiredForNextLevel;
            long beforeLevelUp = grantedXp < firstLevelRequired
                ? firstLevelRequired - grantedXp
                : 0;
            ProgressionSnapshot expectedAfterLevelUp = Player.RebuildProgression(
                checked(beforeLevelUp + grantedXp));
            SetProgression(_secondPlayer, beforeLevelUp);
            Assert(_sidra.ApplyServerDamage(PlayerDamage(_secondPlayer, _sidra.MaxHealth)),
                "Golpe fatal para Level Up foi rejeitado.");
            Assert(_secondPlayer.TotalXp == expectedAfterLevelUp.State.TotalXp
                && _secondPlayer.Level == expectedAfterLevelUp.State.Level
                && _secondPlayer.Reset == expectedAfterLevelUp.State.Reset
                && Player.GetGlobalLevel(_secondPlayer.Reset, _secondPlayer.Level) > 0,
                "Recompensa do Sidra não passou pelo Level Up da progressão.");

            RespawnSidra();
            long level199Start = TotalXpAtGlobalLevel(199);
            ProgressionSnapshot level199 = Player.RebuildProgression(level199Start);
            long nearReset = checked(level199Start + level199.XpRequiredForNextLevel - 10);
            ProgressionSnapshot expectedAfterReset = Player.RebuildProgression(
                checked(nearReset + grantedXp));
            SetProgression(_secondPlayer, nearReset);
            Assert(_secondPlayer.Level == 199 && _secondPlayer.Reset == 0,
                "Preparação do cenário de Reset falhou.");
            Assert(_sidra.ApplyServerDamage(PlayerDamage(_secondPlayer, _sidra.MaxHealth)),
                "Golpe fatal para Reset foi rejeitado.");
            Assert(_secondPlayer.Level == expectedAfterReset.State.Level
                && _secondPlayer.Reset == expectedAfterReset.State.Reset,
                "Recompensa do Sidra não atravessou o Reset.");
            Assert(_secondPlayer.CurrentExperience == expectedAfterReset.XpIntoLevel,
                "XP excedente após Reset não foi preservado.");

            GD.Print("[PASS] Integração Sidra -> killer -> progressão validada.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[SIDRA XP TEST] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private static Player CreatePlayer(
        PackedScene scene,
        Node2D parent,
        int peerId,
        string characterId)
    {
        Player player = scene.Instantiate<Player>();
        player.Name = peerId.ToString();
        player.OwnerPeerId = peerId;
        player.CharacterId = characterId;
        parent.AddChild(player);
        return player;
    }

    private static DamageInfo PlayerDamage(Player player, int amount) =>
        new(DamageSourceType.Player, $"peer {player.OwnerPeerId}", amount,
            player.GlobalPosition, player.OwnerPeerId);

    private void RespawnSidra()
    {
        _sidra.Health.CompleteRespawn();
        Assert(!_sidra.IsDead && _sidra.Health.CurrentHealth == _sidra.MaxHealth,
            "Respawn do Sidra falhou durante o teste.");
    }

    private static void SetProgression(Player player, long totalXp)
    {
        ProgressionSnapshot snapshot = Player.RebuildProgression(totalXp);
        player.TotalXp = snapshot.State.TotalXp;
        player.Level = snapshot.State.Level;
        player.Reset = snapshot.State.Reset;
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
