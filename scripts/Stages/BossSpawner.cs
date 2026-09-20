using Godot;
using System;

public partial class BossSpawner : Node
{
    [Export] public string AreaId { get; set; } = WorldMaps.BearThiefBoss;
    [Export] public float RespawnCooldown { get; set; } = 300.0f;
    [Export] public PackedScene? BossScene { get; set; }
    [Export] public NodePath SpawnPointPath { get; set; } = new("../BossSpawnPoint");
    [Export] public NodePath PlayersPath { get; set; } = new("../../../Players");
    [Export] public NodePath NpcContainerPath { get; set; } = new("../../../StageNPCs");

    public bool IsBossAlive => _rules?.IsAlive ?? false;
    public double RemainingCooldown => _rules?.RemainingCooldownSeconds ?? 0;

    private BossRespawnController? _rules;
    private Marker2D? _spawnPoint;
    private Node2D? _players;
    private Node2D? _npcContainer;
    private long _spawnSequence;

    public override void _Ready()
    {
        try
        {
            _rules = new(RespawnCooldown);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            GD.PushError($"[BOSS SPAWNER] Configuração inválida: {exception.Message}");
            SetProcess(false);
            return;
        }

        _spawnPoint = GetNodeOrNull<Marker2D>(SpawnPointPath);
        _players = GetNodeOrNull<Node2D>(PlayersPath);
        _npcContainer = GetNodeOrNull<Node2D>(NpcContainerPath);
        if (BossScene is null || _spawnPoint is null || _players is null || _npcContainer is null)
        {
            GD.PushError($"[BOSS SPAWNER] Dependências ausentes em {GetPath()}.");
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer() || _rules is null)
            return;

        _rules.Advance(delta);
        if (_rules.CanSpawn(HasPlayersInArea()))
            TrySpawnBoss();
    }

    private void TrySpawnBoss()
    {
        if (_rules is null || BossScene is null || _spawnPoint is null
            || _npcContainer is null || !_rules.TryRegisterSpawn(hasPlayers: true))
        {
            return;
        }

        StageEnemy boss = BossScene.Instantiate<StageEnemy>();
        boss.Name = $"bear_thief_{++_spawnSequence}";
        boss.AreaId = AreaId;
        boss.EnemyId = EnemyRegistry.BearThiefId;
        boss.IsStageBoss = true;
        boss.GlobalPosition = _spawnPoint.GlobalPosition;
        boss.ServerDied += OnBossDied;
        _npcContainer.AddChild(boss, forceReadableName: true);
    }

    private bool HasPlayersInArea()
    {
        foreach (Node child in _players!.GetChildren())
        {
            if (child is Player player && player.MapId == AreaId)
                return true;
        }

        return false;
    }

    private void OnBossDied(StageEnemy boss)
    {
        boss.ServerDied -= OnBossDied;
        _rules?.RegisterDeath();
        boss.CallDeferred(Node.MethodName.QueueFree);
    }
}
