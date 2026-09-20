using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public partial class BearThiefStageIntegrationTest : NetworkManager
{
    private Node2D _players = null!;
    private Node2D _stageNpcs = null!;
    private BearThiefStage _stage = null!;
    private Player _playerA = null!;
    private Player _playerB = null!;
    private Player _playerWithoutDamage = null!;
    private Player _playerWhoLeft = null!;
    private WolfSpawner _wolfSpawner = null!;
    private BossSpawner _bossSpawner = null!;

    public override void _Ready()
    {
        _players = new() { Name = "Players" };
        _stageNpcs = new() { Name = "StageNPCs" };
        AddChild(_players);
        AddChild(_stageNpcs);

        _playerA = CreatePlayer(2, "character-a", WorldMaps.BearThief01);
        _playerB = CreatePlayer(3, "character-b", WorldMaps.KameHouse);
        _playerWithoutDamage = CreatePlayer(4, "character-c", WorldMaps.KameHouse);
        _playerWhoLeft = CreatePlayer(5, "character-d", WorldMaps.KameHouse);

        _stage = GD.Load<PackedScene>("res://scenes/stages/BearThiefStage.tscn")
            .Instantiate<BearThiefStage>();
        _wolfSpawner = _stage.GetNode<WolfSpawner>("BearThief01/WolfSpawner");
        _wolfSpawner.RespawnDelay = 0.08f;
        _bossSpawner = _stage.GetNode<BossSpawner>("BearThiefBoss/BossSpawner");
        _bossSpawner.RespawnCooldown = 0.12f;
        AddChild(_stage);

        _ = RunTestsAsync();
    }

    private async Task RunTestsAsync()
    {
        try
        {
            await WaitFrames(3);
            Assert(RunningAsServer && Multiplayer.IsServer(),
                "O teste precisa executar com autoridade do servidor.");
            ValidateAreasAndTransitions();

            Assert(EnemiesInArea(WorldMaps.BearThief01).Length == 5,
                "Área 01 não preencheu exatamente cinco Wolves.");
            Assert(EnemiesInArea(WorldMaps.BearThief02).Length == 0
                && EnemiesInArea(WorldMaps.BearThief03).Length == 0,
                "Spawner gerou Wolves sem jogadores na área.");
            Assert(_wolfSpawner.AliveCount == 5,
                "Contagem autoritativa do WolfSpawner divergiu.");

            StageEnemy wolf = EnemiesInArea(WorldMaps.BearThief01).First();
            Assert(wolf.Attack == 1200 && wolf.Defense == 900 && wolf.MaxHealth == 1500,
                "Stats configurados do Wolf não foram aplicados.");
            long wolfDamage = new PhysicalDamageCalculator().Calculate(
                _playerA.CurrentCombatStats.Attack,
                wolf.Defense,
                1.0m);
            wolf.Health.CurrentHealth = checked((int)wolfDamage);
            long xpBeforeWolf = _playerA.TotalXp;
            Assert(_playerA.TryApplyServerPhysicalAttack(wolf, 1.0m, out _),
                "Dano físico não matou o Wolf.");
            await WaitFrames(2);
            Assert(_wolfSpawner.AliveCount == 4 && _playerA.TotalXp == xpBeforeWolf + 25,
                "Morte do Wolf não liberou vaga ou não concedeu XP configurado.");
            await WaitSeconds(0.04);
            Assert(_wolfSpawner.AliveCount == 4,
                "Wolf foi reposto antes do RespawnDelay.");
            await WaitSeconds(0.06);
            Assert(_wolfSpawner.AliveCount == 5,
                "Wolf não foi reposto após o RespawnDelay.");

            ValidatePlayerBoundaryTransitions();

            MovePlayersToBossArea();
            await WaitFrames(3);
            StageEnemy[] bosses = EnemiesInArea(WorldMaps.BearThiefBoss);
            Assert(bosses.Length == 1 && _bossSpawner.IsBossAlive,
                "A arena não criou exatamente um Bear Thief compartilhado.");
            StageEnemy boss = bosses[0];
            Assert(boss.Attack == 5000 && boss.Defense == 4500 && boss.MaxHealth == 25000,
                "Stats configurados do Bear Thief não foram aplicados.");

            long xpA = _playerA.TotalXp;
            long xpB = _playerB.TotalXp;
            long xpC = _playerWithoutDamage.TotalXp;
            long xpD = _playerWhoLeft.TotalXp;
            Assert(_playerA.TryApplyServerPhysicalAttack(boss, 1.0m, out _),
                "Participação do jogador A não foi registrada.");
            Assert(_playerB.TryApplyServerPhysicalAttack(boss, 1.0m, out long damageB),
                "Participação do jogador B não foi registrada.");
            Assert(_playerWhoLeft.TryApplyServerPhysicalAttack(boss, 1.0m, out _),
                "Participação do jogador que sairia não foi registrada.");
            _playerWhoLeft.MapId = WorldMaps.BearThief03;
            boss.Health.CurrentHealth = checked((int)damageB);
            Assert(_playerB.TryApplyServerPhysicalAttack(boss, 1.0m, out _),
                "Golpe fatal no Bear Thief foi rejeitado.");
            await WaitFrames(2);

            Assert(_playerA.TotalXp == xpA + 500 && _playerB.TotalXp == xpB + 500,
                "Participantes elegíveis não receberam XP integral do boss.");
            Assert(_playerWithoutDamage.TotalXp == xpC && _playerWhoLeft.TotalXp == xpD,
                "Jogador sem dano ou fora da arena recebeu recompensa.");
            Assert(_playerA.HasCompletedStage(StageIds.BearThief)
                && _playerB.HasCompletedStage(StageIds.BearThief)
                && !_playerWithoutDamage.HasCompletedStage(StageIds.BearThief)
                && !_playerWhoLeft.HasCompletedStage(StageIds.BearThief),
                "Conclusão individual da fase não respeitou participação e presença.");
            Assert(!_playerA.TryCompleteStage(StageIds.BearThief)
                && _playerA.GetCompletedStages().Count == 1,
                "Conclusão da fase não é idempotente.");
            Assert(!_bossSpawner.IsBossAlive && _bossSpawner.RemainingCooldown > 0,
                "Morte do boss não iniciou o cooldown compartilhado.");
            Assert(EnemiesInArea(WorldMaps.BearThiefBoss).Length == 0,
                "Boss permaneceu na arena após a morte.");
            await WaitSeconds(0.06);
            Assert(EnemiesInArea(WorldMaps.BearThiefBoss).Length == 0,
                "Boss reapareceu antes do cooldown.");
            await WaitSeconds(0.09);
            Assert(EnemiesInArea(WorldMaps.BearThiefBoss).Length == 1,
                "Boss não reapareceu após o cooldown.");

            GD.Print("[PASS] Fase Bear Thief validada.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[BEAR THIEF STAGE TEST] {exception.Message}");
            GetTree().Quit(1);
        }
    }

    private void ValidateAreasAndTransitions()
    {
        string[] areaNames = { "BearThief01", "BearThief02", "BearThief03", "BearThiefBoss" };
        foreach (string areaName in areaNames)
        {
            StageArea area = _stage.GetNode<StageArea>(areaName);
            Assert(area.GetNodeOrNull<Node2D>("CleanPath") is not null,
                $"{areaName} não reutiliza CleanPath.");
            Assert(area.GetNodeOrNull<Marker2D>("LeftEntrySpawnPoint") is not null
                && area.GetNodeOrNull<Marker2D>("RightEntrySpawnPoint") is not null,
                $"{areaName} não possui os dois entry spawn points.");
        }

        Assert(StageRoute.TryGetTransition(WorldMaps.BearThief01, 1, out StageTransition right)
            && right.TargetAreaId == WorldMaps.BearThief02
            && right.EntrySide == StageEntrySide.Left,
            "Transição direita da Área 01 está incorreta.");
        Assert(StageRoute.TryGetTransition(WorldMaps.BearThief02, -1, out StageTransition left)
            && left.TargetAreaId == WorldMaps.BearThief01
            && left.EntrySide == StageEntrySide.Right,
            "Transição esquerda da Área 02 está incorreta.");
    }

    private void ValidatePlayerBoundaryTransitions()
    {
        FieldInfo inputField = typeof(Player).GetField(
            "_serverInputDirection",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Input autoritativo não encontrado.");
        MethodInfo transitionMethod = typeof(Player).GetMethod(
            "TryTransitionAtStageBoundary",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Transição autoritativa não encontrada.");

        _playerA.MapId = WorldMaps.BearThief01;
        _playerA.GlobalPosition = new Vector2(WorldMaps.BearThief01Bounds.End.X - 64, 640);
        inputField.SetValue(_playerA, Vector2.Right);
        Assert((bool)transitionMethod.Invoke(_playerA, null)!
            && _playerA.MapId == WorldMaps.BearThief02
            && _playerA.GlobalPosition.IsEqualApprox(
                _stage.GetNode<StageArea>("BearThief02").LeftEntryPosition),
            "Limite direito não levou ao spawn esquerdo da próxima área.");

        _playerA.GlobalPosition = new Vector2(WorldMaps.BearThief02Bounds.Position.X + 64, 640);
        inputField.SetValue(_playerA, Vector2.Left);
        Assert((bool)transitionMethod.Invoke(_playerA, null)!
            && _playerA.MapId == WorldMaps.BearThief01
            && _playerA.GlobalPosition.IsEqualApprox(
                _stage.GetNode<StageArea>("BearThief01").RightEntryPosition),
            "Limite esquerdo não levou ao spawn direito da área anterior.");
    }

    private Player CreatePlayer(int peerId, string characterId, string mapId)
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        player.Name = peerId.ToString();
        player.OwnerPeerId = peerId;
        player.CharacterId = characterId;
        player.MaxHealth = 100_000_000;
        player.BaseBattlePower = 10_000;
        player.MapId = mapId;
        player.Position = WorldMaps.GetArrivalPosition(mapId);
        _players.AddChild(player);
        return player;
    }

    private void MovePlayersToBossArea()
    {
        foreach (Player player in new[]
            { _playerA, _playerB, _playerWithoutDamage, _playerWhoLeft })
        {
            player.MapId = WorldMaps.BearThiefBoss;
            player.GlobalPosition = WorldMaps.BearThiefBossBounds.Position
                + new Vector2(140, 640);
        }
    }

    private StageEnemy[] EnemiesInArea(string areaId) =>
        _stageNpcs.GetChildren().OfType<StageEnemy>()
            .Where(enemy => enemy.AreaId == areaId && !enemy.IsQueuedForDeletion())
            .ToArray();

    private async Task WaitFrames(int count)
    {
        for (int index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task WaitSeconds(double seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
