using Godot;
using System.Collections.Generic;

public partial class Pilaf : NpcBase
{
    private const decimal ContactAttackMultiplier = 1.0m;
    private static readonly StringName AttackAnimation = new("attack");
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");

    [Export] public StringName TargetGroup { get; set; } = new("player");
    [Export] public float ContactDamageInterval { get; set; } = 3.0f;
    [Export] public float AttackVisualDuration { get; set; } = 0.6f;

    private Area2D? _damageArea;
    private CollisionShape2D? _damageShape;
    private Timer? _damageTimer;
    private Node2D? _target;
    private readonly HashSet<Player> _contactTargets = new();
    private static readonly IPhysicalDamageCalculator PhysicalDamageCalculator =
        new PhysicalDamageCalculator();
    private float _attackVisualRemaining;
    private bool _combatSignalsConnected;

    public override void _Ready()
    {
        base._Ready();

        _damageArea = GetNodeOrNull<Area2D>("DamageArea");
        _damageShape = GetNodeOrNull<CollisionShape2D>("DamageArea/CollisionShape2D");
        _damageTimer = GetNodeOrNull<Timer>("DamageTimer");

        if (AnimatedSprite is not null)
            AnimatedSprite.AnimationFinished += OnAnimationFinished;

        if (!CanRunServerAi())
        {
            _damageTimer?.Stop();
            return;
        }

        if (_damageArea is null || _damageShape is null || _damageTimer is null)
        {
            GD.PushError($"[SERVER] Nodes de combate do Pilaf ausentes: {GetPath()}");
        }
        else
        {
            _damageArea.BodyEntered += OnDamageAreaBodyEntered;
            _damageArea.BodyExited += OnDamageAreaBodyExited;
            _damageTimer.Timeout += OnDamageTimerTimeout;
            _combatSignalsConnected = true;
            _damageTimer.WaitTime = Mathf.Max(ContactDamageInterval, 0.05f);
            _damageTimer.OneShot = false;
        }

        FindTarget();
        LogServerAiActive();
    }

    public override void _ExitTree()
    {
        if (_combatSignalsConnected && _damageArea is not null)
        {
            _damageArea.BodyEntered -= OnDamageAreaBodyEntered;
            _damageArea.BodyExited -= OnDamageAreaBodyExited;
        }

        if (_combatSignalsConnected && _damageTimer is not null)
        {
            _damageTimer.Stop();
            _damageTimer.Timeout -= OnDamageTimerTimeout;
        }
        _combatSignalsConnected = false;

        if (AnimatedSprite is not null)
            AnimatedSprite.AnimationFinished -= OnAnimationFinished;

        base._ExitTree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!CanRunServerAi())
        {
            UpdateClientPresentation();
            UpdateAttackPresentation();
            return;
        }

        UpdateServerAttackState((float)delta);

        if (!CanAct)
            return;

        if (IsAttacking)
        {
            Velocity = Vector2.Zero;
            UpdateServerMovementState(Vector2.Zero);
            UpdateAttackPresentation();
            return;
        }

        if (!IsValidTarget(_target))
            FindTarget();

        if (!IsValidTarget(_target))
        {
            Velocity = Vector2.Zero;
            UpdateServerMovementState(Vector2.Zero);
            UpdateAttackPresentation();
            return;
        }

        MovementDirection = GlobalPosition.DirectionTo(_target!.GlobalPosition).LimitLength(1.0f);
        Velocity = MovementDirection * MoveSpeed;
        MoveAndSlide();
        KeepInsideViewport();
        UpdateServerMovementState(MovementDirection);
        UpdateAttackPresentation();
    }

    private void FindTarget()
    {
        if (!CanRunServerAi() || !CanAct)
            return;

        Player? nearestPlayer = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup(TargetGroup))
        {
            if (node is not Player player || !player.CanAct
                || !WorldMaps.GetBoundsAt(GlobalPosition).HasPoint(player.GlobalPosition))
                continue;

            float distanceSquared = GlobalPosition.DistanceSquaredTo(player.GlobalPosition);
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearestPlayer = player;
        }

        _target = nearestPlayer;
    }

    private void OnDamageAreaBodyEntered(Node2D body)
    {
        if (!CanRunServerAi() || !CanAct || body is not Player player || !player.CanAct)
            return;

        _contactTargets.Add(player);
        TryDealServerPhysicalContactDamage(player, out _);
        _damageTimer?.Start(Mathf.Max(ContactDamageInterval, 0.05f));
    }

    private void OnDamageAreaBodyExited(Node2D body)
    {
        if (!CanRunServerAi() || body is not Player player)
            return;

        _contactTargets.Remove(player);
        if (_contactTargets.Count == 0)
            _damageTimer?.Stop();
    }

    private void OnDamageTimerTimeout()
    {
        if (!CanRunServerAi() || !CanAct)
            return;

        Player? target = GetNearestContactTarget();
        if (target is null)
        {
            _damageTimer?.Stop();
            return;
        }

        TryDealServerPhysicalContactDamage(target, out _);
    }

    private Player? GetNearestContactTarget()
    {
        Player? nearestPlayer = null;
        float nearestDistanceSquared = float.MaxValue;
        List<Player> invalidPlayers = new();

        foreach (Player player in _contactTargets)
        {
            if (!GodotObject.IsInstanceValid(player) || !player.CanAct
                || !WorldMaps.GetBoundsAt(GlobalPosition).HasPoint(player.GlobalPosition))
            {
                invalidPlayers.Add(player);
                continue;
            }

            float distanceSquared = GlobalPosition.DistanceSquaredTo(player.GlobalPosition);
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearestPlayer = player;
        }

        foreach (Player invalidPlayer in invalidPlayers)
            _contactTargets.Remove(invalidPlayer);

        return nearestPlayer;
    }

    internal bool TryDealServerPhysicalContactDamage(Player target, out long damage)
    {
        damage = 0;
        if (!CanRunServerAi() || !CanAct || !target.CanAct)
            return false;

        try
        {
            damage = PhysicalDamageCalculator.Calculate(
                Attack,
                target.Defense,
                ContactAttackMultiplier);
        }
        catch (System.ArgumentOutOfRangeException exception)
        {
            GD.PushError(
                $"[SERVER][COMBAT] Stats físicos inválidos no ataque de {Name}: "
                + exception.Message);
            return false;
        }

#if DEBUG
        GD.Print(
            $"[COMBAT] Atacante {Name} | Attack: {Attack} | "
            + $"Defense do alvo: {target.Defense} | "
            + $"Multiplicador: {ContactAttackMultiplier} | Dano: {damage}");
#endif

        DamageInfo damageInfo = new(
            DamageSourceType.Npc,
            Name,
            damage,
            GlobalPosition
        );

        if (!target.ApplyServerDamage(damageInfo))
            return false;

        SetServerAttacking(true);
        _attackVisualRemaining = Mathf.Max(AttackVisualDuration, 0.05f);
        UpdateAttackPresentation();
        return true;
    }

    private void UpdateServerAttackState(float delta)
    {
        if (!IsAttacking)
            return;

        _attackVisualRemaining = Mathf.Max(_attackVisualRemaining - delta, 0.0f);
        if (_attackVisualRemaining <= 0.0f)
            SetServerAttacking(false);
    }

    private bool IsValidTarget(Node2D? target)
    {
        return GodotObject.IsInstanceValid(target) && target is Player player && player.CanAct
            && WorldMaps.GetBoundsAt(GlobalPosition).HasPoint(player.GlobalPosition);
    }

    private void UpdateAttackPresentation()
    {
        if (AnimatedSprite is null || IsDead || IsRespawning)
            return;

        if (IsAttacking)
        {
            if (AnimatedSprite.Animation != AttackAnimation || !AnimatedSprite.IsPlaying())
                AnimatedSprite.Play(AttackAnimation);
            return;
        }

        StringName movementAnimation =
            AiState == MovingAiState ? WalkAnimation : IdleAnimation;
        if (AnimatedSprite.Animation != movementAnimation || !AnimatedSprite.IsPlaying())
            AnimatedSprite.Play(movementAnimation);
    }

    private void OnAnimationFinished()
    {
        if (AnimatedSprite is null || AnimatedSprite.Animation != AttackAnimation || IsAttacking)
            return;

        UpdateAttackPresentation();
    }

    protected override void OnDied()
    {
        _target = null;
        _contactTargets.Clear();
        _damageShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _damageArea?.SetDeferred(Area2D.PropertyName.Monitoring, false);
        _damageTimer?.Stop();
        _attackVisualRemaining = 0.0f;
        SetServerAttacking(false);
    }

    protected override void OnRespawned()
    {
        if (!CanRunServerAi())
            return;

        _target = null;
        _contactTargets.Clear();
        _damageShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _damageArea?.SetDeferred(Area2D.PropertyName.Monitoring, true);
        _damageTimer?.Stop();
        _attackVisualRemaining = 0.0f;
        SetServerAttacking(false);
        FindTarget();
        UpdateAttackPresentation();
    }
}
