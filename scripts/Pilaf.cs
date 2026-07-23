using Godot;
using System.Collections.Generic;

public partial class Pilaf : NpcBase
{
    private static readonly Vector2 NormalVisualScale = Vector2.One;
    private static readonly Vector2 AttackVisualScale = new(0.5f, 0.5f);
    private static readonly Vector2 NormalVisualOffset = Vector2.Zero;
    private static readonly Vector2 AttackVisualOffset = new(0.0f, 91.0f);
    private static readonly StringName AttackAnimation = new("attack");
    private static readonly StringName IdleAnimation = new("idle");

    [Export] public StringName TargetGroup { get; set; } = new("player");
    [Export] public int ContactDamage { get; set; } = 10;
    [Export] public float ContactDamageInterval { get; set; } = 3.0f;
    [Export] public float AttackVisualDuration { get; set; } = 0.6f;

    private Area2D? _damageArea;
    private CollisionShape2D? _damageShape;
    private Timer? _damageTimer;
    private Node2D? _target;
    private readonly HashSet<Player> _contactTargets = new();
    private float _attackVisualRemaining;

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
            _damageTimer.WaitTime = Mathf.Max(ContactDamageInterval, 0.05f);
            _damageTimer.OneShot = false;
        }

        FindTarget();
        LogServerAiActive();
    }

    public override void _ExitTree()
    {
        if (_damageArea is not null)
        {
            _damageArea.BodyEntered -= OnDamageAreaBodyEntered;
            _damageArea.BodyExited -= OnDamageAreaBodyExited;
        }

        if (_damageTimer is not null)
        {
            _damageTimer.Stop();
            _damageTimer.Timeout -= OnDamageTimerTimeout;
        }

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
            if (node is not Player player || !player.CanAct)
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
        DealContactDamage(player);
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

        DealContactDamage(target);
    }

    private Player? GetNearestContactTarget()
    {
        Player? nearestPlayer = null;
        float nearestDistanceSquared = float.MaxValue;
        List<Player> invalidPlayers = new();

        foreach (Player player in _contactTargets)
        {
            if (!GodotObject.IsInstanceValid(player) || !player.CanAct)
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

    private void DealContactDamage(Player target)
    {
        if (!CanRunServerAi() || !CanAct || !target.CanAct)
            return;

        if (ContactDamage <= 0 || ContactDamage > 100)
        {
            GD.PushError($"[SERVER][COMBAT] Dano de contato inválido no Pilaf: {ContactDamage}");
            return;
        }

        DamageInfo damageInfo = new(
            DamageSourceType.Npc,
            Name,
            ContactDamage,
            GlobalPosition
        );

        if (!target.ApplyServerDamage(damageInfo))
            return;

        SetServerAttacking(true);
        _attackVisualRemaining = Mathf.Max(AttackVisualDuration, 0.05f);
        UpdateAttackPresentation();
    }

    private void UpdateServerAttackState(float delta)
    {
        if (!IsAttacking)
            return;

        _attackVisualRemaining = Mathf.Max(_attackVisualRemaining - delta, 0.0f);
        if (_attackVisualRemaining <= 0.0f)
            SetServerAttacking(false);
    }

    private static bool IsValidTarget(Node2D? target)
    {
        return GodotObject.IsInstanceValid(target) && target is Player player && player.CanAct;
    }

    private void UpdateAttackPresentation()
    {
        if (AnimatedSprite is null || IsDead || IsRespawning)
            return;

        if (IsAttacking)
        {
            AnimatedSprite.Scale = AttackVisualScale;
            AnimatedSprite.Position = AttackVisualOffset;
            if (AnimatedSprite.Animation != AttackAnimation || !AnimatedSprite.IsPlaying())
                AnimatedSprite.Play(AttackAnimation);
            return;
        }

        AnimatedSprite.Scale = NormalVisualScale;
        AnimatedSprite.Position = NormalVisualOffset;
        if (AnimatedSprite.Animation != IdleAnimation)
            AnimatedSprite.Play(IdleAnimation);
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
