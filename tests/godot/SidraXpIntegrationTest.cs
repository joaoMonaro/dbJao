using Godot;
using System;
using System.Reflection;

public partial class SidraXpIntegrationTest : NetworkManager
{
    private Player _firstPlayer = null!;
    private Player _secondPlayer = null!;
    private Sidra _sidra = null!;
    private Pilaf _pilaf = null!;

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

        _pilaf = GD.Load<PackedScene>("res://scenes/Pilaf.tscn").Instantiate<Pilaf>();
        _pilaf.Name = "Pilaf";
        _pilaf.Position = new Vector2(500, 500);
        npcs.AddChild(_pilaf);

        CallDeferred(MethodName.RunTests);
    }

    private void RunTests()
    {
        try
        {
            Assert(RunningAsServer && Multiplayer.IsServer(),
                "O teste precisa executar como servidor.");
            Assert(_sidra.Defense == 10,
                "Defense inicial do Sidra não corresponde à configuração da cena.");
            MethodInfo? attackRequest = typeof(Player).GetMethod(
                "RequestAttack",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(attackRequest is not null && attackRequest.GetParameters().Length == 0,
                "O RPC de ataque não deve aceitar dano informado pelo cliente.");

            int playerHealthBeforeNpcAttack = _firstPlayer.CurrentHealth;
            long expectedNpcDamage = new PhysicalDamageCalculator().Calculate(
                _pilaf.Attack,
                _firstPlayer.Defense,
                1.0m);
            Assert(_pilaf.Attack == 17 && _pilaf.Defense == 10,
                "Attack/Defense iniciais do Pilaf não correspondem à configuração da cena.");
            Assert(_pilaf.TryDealServerPhysicalContactDamage(
                    _firstPlayer,
                    out long npcDamage)
                && npcDamage == expectedNpcDamage
                && _firstPlayer.CurrentHealth == playerHealthBeforeNpcAttack - npcDamage,
                "Pilaf Attack vs Player Defense não utilizou o calculador físico.");
            Assert(_firstPlayer.Health.Heal(checked((int)npcDamage)),
                "Não foi possível restaurar a vida após validar o ataque do Pilaf.");

            long expectedFirstDamage = new PhysicalDamageCalculator().Calculate(
                _firstPlayer.CurrentCombatStats.Attack,
                _sidra.Defense,
                1.0m);
            int healthBeforeFirstHit = _sidra.Health.CurrentHealth;
            Assert(_firstPlayer.TryApplyServerPhysicalAttack(_sidra, 1.0m, out long firstDamage),
                "Ataque físico válido do jogador deveria ser aplicado ao Sidra.");
            Assert(firstDamage == expectedFirstDamage
                && _sidra.Health.CurrentHealth == healthBeforeFirstHit - firstDamage,
                "Player Attack vs Sidra Defense não utilizou o calculador físico.");
            Assert(!_sidra.IsDead, "Sidra morreu antes do golpe fatal.");
            Assert(_firstPlayer.TotalXp == 0 && _secondPlayer.TotalXp == 0,
                "Sidra concedeu XP enquanto ainda estava vivo.");
            Assert(_firstPlayer.BaseBattlePower == 10 && _secondPlayer.BaseBattlePower == 10,
                "Sidra vivo alterou o Poder de Luta.");

            _sidra.Health.CurrentHealth = checked((int)expectedFirstDamage);
            Assert(_secondPlayer.TryApplyServerPhysicalAttack(
                    _sidra,
                    1.0m,
                    out long killingDamage)
                && killingDamage == expectedFirstDamage,
                "Golpe físico fatal deveria ser calculado e aceito.");
            Assert(_sidra.IsDead, "Servidor não confirmou a morte do Sidra.");
            Assert(_firstPlayer.TotalXp == 0, "XP foi concedido ao jogador incorreto.");
            long grantedXp = _secondPlayer.TotalXp;
            Assert(grantedXp > 0, "Golpe fatal não concedeu XP.");
            long firstCompletedLevels = Player.GetGlobalLevel(
                _secondPlayer.Reset, _secondPlayer.Level);
            Assert(_secondPlayer.BaseBattlePower == 10 + firstCompletedLevels * 100,
                "XP do Sidra alterou incorretamente o Poder de Luta.");
            Assert(!_secondPlayer.TryApplyServerPhysicalAttack(_sidra, 1.0m, out _),
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
            long battlePowerBeforeLevelUp = _secondPlayer.BaseBattlePower;
            long globalBeforeLevelUp = Player.GetGlobalLevel(
                _secondPlayer.Reset, _secondPlayer.Level);
            Assert(KillSidraWithPhysicalAttack(_secondPlayer),
                "Golpe fatal para Level Up foi rejeitado.");
            Assert(_secondPlayer.TotalXp == expectedAfterLevelUp.State.TotalXp
                && _secondPlayer.Level == expectedAfterLevelUp.State.Level
                && _secondPlayer.Reset == expectedAfterLevelUp.State.Reset
                && Player.GetGlobalLevel(_secondPlayer.Reset, _secondPlayer.Level) > 0,
                "Recompensa do Sidra não passou pelo Level Up da progressão.");
            long completedByReward = Player.GetGlobalLevel(
                _secondPlayer.Reset, _secondPlayer.Level) - globalBeforeLevelUp;
            Assert(_secondPlayer.BaseBattlePower
                    == battlePowerBeforeLevelUp + completedByReward * 100,
                "Level Up causado pelo Sidra não concedeu Poder de Luta corretamente.");

            RespawnSidra();
            long level199Start = TotalXpAtGlobalLevel(199);
            ProgressionSnapshot level199 = Player.RebuildProgression(level199Start);
            long nearReset = checked(level199Start + level199.XpRequiredForNextLevel - 10);
            ProgressionSnapshot expectedAfterReset = Player.RebuildProgression(
                checked(nearReset + grantedXp));
            SetProgression(_secondPlayer, nearReset);
            long battlePowerBeforeReset = _secondPlayer.BaseBattlePower;
            long globalBeforeReset = Player.GetGlobalLevel(
                _secondPlayer.Reset, _secondPlayer.Level);
            Assert(_secondPlayer.Level == 199 && _secondPlayer.Reset == 0,
                "Preparação do cenário de Reset falhou.");
            Assert(KillSidraWithPhysicalAttack(_secondPlayer),
                "Golpe fatal para Reset foi rejeitado.");
            Assert(_secondPlayer.Level == expectedAfterReset.State.Level
                && _secondPlayer.Reset == expectedAfterReset.State.Reset,
                "Recompensa do Sidra não atravessou o Reset.");
            Assert(_secondPlayer.CurrentExperience == expectedAfterReset.XpIntoLevel,
                "XP excedente após Reset não foi preservado.");
            long levelsAcrossReset = Player.GetGlobalLevel(
                _secondPlayer.Reset, _secondPlayer.Level) - globalBeforeReset;
            Assert(_secondPlayer.BaseBattlePower
                    == battlePowerBeforeReset + levelsAcrossReset * 100,
                "Sidra concedeu bônus incorreto na travessia de Reset.");

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

    private bool KillSidraWithPhysicalAttack(Player player)
    {
        long damage = new PhysicalDamageCalculator().Calculate(
            player.CurrentCombatStats.Attack,
            _sidra.Defense,
            1.0m);
        if (damage < _sidra.Health.CurrentHealth)
            _sidra.Health.CurrentHealth = checked((int)damage);

        return player.TryApplyServerPhysicalAttack(_sidra, 1.0m, out long appliedDamage)
            && appliedDamage == damage;
    }

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
        long completedLevels = Player.GetGlobalLevel(snapshot.State.Reset, snapshot.State.Level);
        player.BaseBattlePower = new BattlePowerProgression(BattlePowerSettings.Default)
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
