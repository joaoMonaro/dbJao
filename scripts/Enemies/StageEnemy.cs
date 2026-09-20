using Godot;
using System;
using System.Collections.Generic;

public partial class StageEnemy : NpcBase
{
    private const decimal PhysicalAttackMultiplier = 1.0m;
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");
    private static readonly StringName AttackAnimation = new("attack");
    private static readonly IPhysicalDamageCalculator PhysicalDamageCalculator =
        new PhysicalDamageCalculator();

    [Export] public string EnemyId { get; set; } = EnemyRegistry.WolfId;
    [Export] public bool IsStageBoss { get; set; }

    public event Action<StageEnemy>? ServerDied;
    public EnemyDefinition Definition { get; private set; } = null!;
    public long XpReward => Definition.XpReward;

    private readonly BossParticipationTracker _participation = new();
    private Player? _target;
    private float _attackCooldownRemaining;
    private float _attackVisualRemaining;

    public override void _Ready()
    {
        try
        {
            Definition = EnemyRegistry.Get(EnemyId);
            EnemyCombatStats stats = Definition.CalculateCombatStats();
            MaxHealth = stats.MaxHealth;
            Attack = stats.Attack;
            Defense = stats.Defense;
            MoveSpeed = Definition.MoveSpeed;
            RespawnDelay = IsStageBoss ? 300.0f : 3.0f;
        }
        catch (Exception exception) when (
            exception is ArgumentException or KeyNotFoundException or OverflowException)
        {
            GD.PushError($"[ENEMY] Configuração inválida em {GetPath()}: {exception.Message}");
            SetPhysicsProcess(false);
            return;
        }

        base._Ready();
        UpdateAnimation();
        if (CanRunServerAi())
            LogServerAiActive();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!CanRunServerAi())
        {
            UpdateClientPresentation();
            UpdateAnimation();
            return;
        }

        float frameDelta = (float)delta;
        _attackCooldownRemaining = Mathf.Max(0, _attackCooldownRemaining - frameDelta);
        _attackVisualRemaining = Mathf.Max(0, _attackVisualRemaining - frameDelta);
        if (_attackVisualRemaining <= 0 && IsAttacking)
            SetServerAttacking(false);

        if (!CanAct)
            return;

        if (!IsValidTarget(_target))
            _target = FindNearestTarget();

        if (!IsValidTarget(_target))
        {
            Velocity = Vector2.Zero;
            UpdateServerMovementState(Vector2.Zero);
            UpdateAnimation();
            return;
        }

        float distance = GlobalPosition.DistanceTo(_target!.GlobalPosition);
        if (distance <= Definition.AttackRange)
        {
            Velocity = Vector2.Zero;
            UpdateServerMovementState(Vector2.Zero);
            if (_attackCooldownRemaining <= 0)
                TryAttack(_target);
        }
        else
        {
            Vector2 direction = GlobalPosition.DirectionTo(_target.GlobalPosition);
            Velocity = direction * MoveSpeed;
            MoveAndSlide();
            KeepInsideViewport();
            UpdateServerMovementState(direction);
        }

        UpdateAnimation();
    }

    protected override void OnDamageApplied(DamageInfo damageInfo)
    {
        if (IsStageBoss && damageInfo.SourceType == DamageSourceType.Player
            && damageInfo.AttackerPeerId is > NetworkConstants.ServerPeerId and <= int.MaxValue)
        {
            _participation.Register(checked((int)damageInfo.AttackerPeerId));
        }
    }

    protected override void OnKilled(DamageInfo killingBlow)
    {
        if (!CanRunServerAi())
            return;

        if (IsStageBoss)
        {
            ResolveBossRewards();
            return;
        }

        if (TryResolvePlayer(killingBlow.AttackerPeerId, out Player? killer))
            GrantXp(killer);
    }

    protected override void OnDied()
    {
        _target = null;
        Velocity = Vector2.Zero;
        ServerDied?.Invoke(this);
    }

    private Player? FindNearestTarget()
    {
        Player? nearest = null;
        float nearestDistanceSquared = Definition.DetectionRange * Definition.DetectionRange;
        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is not Player player || !player.CanAct || player.MapId != AreaId)
                continue;

            float distanceSquared = GlobalPosition.DistanceSquaredTo(player.GlobalPosition);
            if (distanceSquared > nearestDistanceSquared)
                continue;

            nearest = player;
            nearestDistanceSquared = distanceSquared;
        }

        return nearest;
    }

    private bool IsValidTarget(Player? player) =>
        GodotObject.IsInstanceValid(player) && player!.CanAct && player.MapId == AreaId
        && GlobalPosition.DistanceSquaredTo(player.GlobalPosition)
            <= Definition.DetectionRange * Definition.DetectionRange;

    private void TryAttack(Player player)
    {
        _attackCooldownRemaining = Definition.AttackCooldown;
        long damage;
        try
        {
            damage = PhysicalDamageCalculator.Calculate(
                Attack,
                player.Defense,
                PhysicalAttackMultiplier);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            GD.PushError($"[COMBAT] Stats inválidos para {Name}: {exception.Message}");
            return;
        }

        DamageInfo damageInfo = new(DamageSourceType.Npc, EnemyId, damage, GlobalPosition);
        if (!player.ApplyServerDamage(damageInfo))
            return;

        SetServerAttacking(true);
        _attackVisualRemaining = Mathf.Min(Definition.AttackCooldown, 0.6f);
    }

    private void ResolveBossRewards()
    {
        IReadOnlyList<int> eligible = _participation.GetEligibleParticipants(peerId =>
            TryResolvePlayer(peerId, out Player? player) && player.MapId == AreaId);

        foreach (int peerId in eligible)
        {
            if (!TryResolvePlayer(peerId, out Player? player))
                continue;

            GrantXp(player);
            if (GetTree().CurrentScene is not NetworkManager manager
                || !manager.CompleteStageForPlayer(player, StageIds.BearThief))
            {
                player.TryCompleteStage(StageIds.BearThief);
            }
        }

        GD.Print(
            $"[STAGE] Bear Thief derrotado; {eligible.Count} participante(s) elegível(is).");
    }

    private void GrantXp(Player player)
    {
        if (XpReward <= 0 || !player.AddXp(XpReward))
        {
            GD.PushWarning($"[XP] Recompensa de {EnemyId} não pôde ser concedida.");
            return;
        }

        GD.Print($"[XP] {EnemyId} concedeu {XpReward} XP para {player.CharacterId}.");
    }

    private bool TryResolvePlayer(long peerId, out Player player)
    {
        player = null!;
        if (peerId is <= NetworkConstants.ServerPeerId or > int.MaxValue)
            return false;

        Node? players = GetTree().CurrentScene?.GetNodeOrNull("Players");
        Player? resolved = players?.GetNodeOrNull<Player>(checked((int)peerId).ToString());
        if (resolved is null || resolved.OwnerPeerId != peerId
            || string.IsNullOrWhiteSpace(resolved.CharacterId))
        {
            return false;
        }

        player = resolved;
        return true;
    }

    private void UpdateAnimation()
    {
        if (AnimatedSprite is null || IsDead || IsRespawning)
            return;

        StringName animation = IsAttacking
            ? AttackAnimation
            : AiState == MovingAiState ? WalkAnimation : IdleAnimation;
        if (AnimatedSprite.Animation != animation || !AnimatedSprite.IsPlaying())
            AnimatedSprite.Play(animation);
    }
}
