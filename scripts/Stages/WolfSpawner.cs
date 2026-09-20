using Godot;
using System;
using System.Collections.Generic;

public partial class WolfSpawner : Node
{
    [Export] public string AreaId { get; set; } = string.Empty;
    [Export] public int MaxAlive { get; set; } = 5;
    [Export] public float RespawnDelay { get; set; } = 3.0f;
    [Export] public PackedScene? WolfScene { get; set; }
    [Export] public NodePath SpawnPointsPath { get; set; } = new("../WolfSpawnPoints");
    [Export] public NodePath PlayersPath { get; set; } = new("../../../Players");
    [Export] public NodePath NpcContainerPath { get; set; } = new("../../../StageNPCs");

    public int AliveCount => _rules?.AliveCount ?? 0;

    private RespawnSlotController? _rules;
    private Node? _spawnPoints;
    private Node2D? _players;
    private Node2D? _npcContainer;
    private readonly List<Marker2D> _markers = [];
    private int _nextMarkerIndex;
    private long _spawnSequence;
    private bool _activated;

    public override void _Ready()
    {
        try
        {
            _rules = new(MaxAlive, RespawnDelay);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            GD.PushError($"[WOLF SPAWNER] Configuração inválida: {exception.Message}");
            SetProcess(false);
            return;
        }

        _spawnPoints = GetNodeOrNull(SpawnPointsPath);
        _players = GetNodeOrNull<Node2D>(PlayersPath);
        _npcContainer = GetNodeOrNull<Node2D>(NpcContainerPath);
        if (_spawnPoints is not null)
        {
            foreach (Node child in _spawnPoints.GetChildren())
            {
                if (child is Marker2D marker)
                    _markers.Add(marker);
            }
        }

        if (WolfScene is null || _players is null || _npcContainer is null || _markers.Count == 0)
        {
            GD.PushError($"[WOLF SPAWNER] Dependências ausentes em {GetPath()}.");
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer() || _rules is null)
            return;

        bool hasPlayers = HasPlayersInArea();
        if (!_activated && hasPlayers)
        {
            int initialCount = _rules.GetInitialSpawnCount(hasPlayers);
            for (int index = 0; index < initialCount; index++)
            {
                if (!TrySpawnOne())
                    break;
            }
            _activated = true;
            return;
        }

        if (_activated && _rules.Advance(delta, hasPlayers))
            TrySpawnOne();
    }

    private bool TrySpawnOne()
    {
        if (_rules is null || WolfScene is null || _npcContainer is null
            || _rules.AliveCount >= _rules.MaxAlive)
        {
            return false;
        }

        Marker2D? marker = FindAvailableMarker();
        if (marker is null)
            return false;

        StageEnemy wolf = WolfScene.Instantiate<StageEnemy>();
        wolf.Name = $"{AreaId}_wolf_{++_spawnSequence}";
        wolf.AreaId = AreaId;
        wolf.EnemyId = EnemyRegistry.WolfId;
        wolf.IsStageBoss = false;
        wolf.GlobalPosition = marker.GlobalPosition;
        wolf.ServerDied += OnWolfDied;
        _npcContainer.AddChild(wolf, forceReadableName: true);

        if (!_rules.TryRegisterSpawn())
        {
            wolf.ServerDied -= OnWolfDied;
            wolf.QueueFree();
            return false;
        }

        return true;
    }

    private Marker2D? FindAvailableMarker()
    {
        const float minimumPlayerDistanceSquared = 96.0f * 96.0f;
        for (int attempt = 0; attempt < _markers.Count; attempt++)
        {
            Marker2D marker = _markers[_nextMarkerIndex++ % _markers.Count];
            bool occupied = false;
            foreach (Node child in _players!.GetChildren())
            {
                if (child is Player player && player.MapId == AreaId
                    && player.GlobalPosition.DistanceSquaredTo(marker.GlobalPosition)
                        < minimumPlayerDistanceSquared)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
            {
                foreach (Node child in _npcContainer!.GetChildren())
                {
                    if (child is StageEnemy enemy && enemy.AreaId == AreaId
                        && enemy.CanAct
                        && enemy.GlobalPosition.DistanceSquaredTo(marker.GlobalPosition)
                            < minimumPlayerDistanceSquared)
                    {
                        occupied = true;
                        break;
                    }
                }
            }

            if (!occupied)
                return marker;
        }

        return null;
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

    private void OnWolfDied(StageEnemy wolf)
    {
        wolf.ServerDied -= OnWolfDied;
        _rules?.RegisterDeath();
        wolf.CallDeferred(Node.MethodName.QueueFree);
    }
}
