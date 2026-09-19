using Godot;

public partial class Sidra : NpcBase
{
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");
    private static readonly StringName AttackAnimation = new("attack");
    private static readonly Vector2 PreviousSpriteHalfExtent = new(41.0f, 75.5f);

    [Export] public float MinDirectionTime { get; set; } = 1.5f;
    [Export] public float MaxDirectionTime { get; set; } = 4.0f;
    [Export] public long XpReward { get; set; } = 25;

    private static readonly Vector2[] PossibleDirections =
    {
        Vector2.Zero,
        Vector2.Zero,
        Vector2.Up,
        Vector2.Down,
        Vector2.Left,
        Vector2.Right,
        new Vector2(-1.0f, -1.0f).Normalized(),
        new Vector2(1.0f, -1.0f).Normalized(),
        new Vector2(-1.0f, 1.0f).Normalized(),
        new Vector2(1.0f, 1.0f).Normalized(),
    };

    private readonly RandomNumberGenerator _random = new();
    private float _directionTimer;

    public override void _Ready()
    {
        base._Ready();
        UpdateAnimation();

        if (!CanRunServerAi())
            return;

        _random.Randomize();
        ChooseNewDirection();
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

        if (IsDead)
            return;

        _directionTimer -= (float)delta;
        if (_directionTimer <= 0.0f)
            ChooseNewDirection();

        Velocity = MovementDirection * MoveSpeed;
        MoveAndSlide();

        Vector2 avoidanceDirection = GetCollisionAvoidanceDirection();
        avoidanceDirection += KeepInsideViewport(PreviousSpriteHalfExtent);

        if (avoidanceDirection != Vector2.Zero)
            ChooseNewDirection(avoidanceDirection.Normalized());
        else if (GetSlideCollisionCount() > 0)
            ChooseNewDirection();

        UpdateServerMovementState(MovementDirection);
        UpdateAnimation();
    }

    protected override void OnRespawned()
    {
        if (!CanRunServerAi())
            return;

        ChooseNewDirection();
        UpdateServerMovementState(MovementDirection);
        UpdateAnimation();
    }

    protected override void OnKilled(DamageInfo killingBlow)
    {
        if (!CanRunServerAi() || killingBlow.SourceType != DamageSourceType.Player)
            return;

        if (XpReward <= 0)
        {
            GD.PushWarning($"[XP] Recompensa inválida configurada para {Name}: {XpReward}");
            return;
        }

        if (killingBlow.AttackerPeerId is <= NetworkConstants.ServerPeerId or > int.MaxValue)
        {
            GD.PushWarning($"[XP] Sidra morreu sem peer atacante válido.");
            return;
        }

        int attackerPeerId = checked((int)killingBlow.AttackerPeerId);
        Player? killer = GetParent()?.GetParent()
            ?.GetNodeOrNull<Player>($"Players/{attackerPeerId}");
        if (killer is null || killer.OwnerPeerId != attackerPeerId
            || string.IsNullOrWhiteSpace(killer.CharacterId))
        {
            GD.PushWarning($"[XP] Killer do Sidra não corresponde a um jogador autenticado.");
            return;
        }

        long previousTotalXp = killer.TotalXp;
        int previousLevel = killer.Level;
        long previousReset = killer.Reset;
        if (!killer.AddXp(XpReward))
        {
            GD.PushWarning($"[XP] Não foi possível conceder a recompensa do Sidra.");
            return;
        }

        GD.Print($"[XP] Sidra morto pelo personagem {killer.CharacterId}.");
        GD.Print($"[XP] Recompensa: {XpReward}");
        GD.Print($"[XP] TotalXp: {previousTotalXp} -> {killer.TotalXp}");
        GD.Print($"[XP] Level: {previousLevel} -> {killer.Level}");
        GD.Print($"[XP] Reset: {previousReset} -> {killer.Reset}");
    }

    private void UpdateAnimation()
    {
        if (AnimatedSprite is null || IsDead || IsRespawning)
            return;

        StringName animation = IsAttacking
            ? AttackAnimation
            : AiState == MovingAiState ? WalkAnimation : IdleAnimation;

        if (AnimatedSprite.Animation != animation)
            AnimatedSprite.Play(animation);
        else if (animation != AttackAnimation && !AnimatedSprite.IsPlaying())
            AnimatedSprite.Play(animation);
    }

    private Vector2 GetCollisionAvoidanceDirection()
    {
        Vector2 avoidanceDirection = Vector2.Zero;

        for (int collisionIndex = 0; collisionIndex < GetSlideCollisionCount(); collisionIndex++)
        {
            KinematicCollision2D collision = GetSlideCollision(collisionIndex);
            avoidanceDirection += collision.GetNormal();
        }

        return avoidanceDirection != Vector2.Zero
            ? avoidanceDirection.Normalized()
            : Vector2.Zero;
    }

    private void ChooseNewDirection(Vector2 inwardDirection = default)
    {
        if (!CanRunServerAi())
            return;

        Vector2 candidate = Vector2.Zero;
        bool candidateFound = false;

        for (int attempt = 0; attempt < PossibleDirections.Length; attempt++)
        {
            candidate = PossibleDirections[_random.RandiRange(0, PossibleDirections.Length - 1)];
            if (
                inwardDirection == Vector2.Zero
                || candidate == Vector2.Zero
                || candidate.Dot(inwardDirection) >= 0.0f
            )
            {
                candidateFound = true;
                break;
            }
        }

        if (!candidateFound)
            candidate = inwardDirection.Normalized();

        MovementDirection = candidate != Vector2.Zero ? candidate.Normalized() : Vector2.Zero;

        float minimumTime = Mathf.Min(MinDirectionTime, MaxDirectionTime);
        float maximumTime = Mathf.Max(MinDirectionTime, MaxDirectionTime);
        _directionTimer = _random.RandfRange(
            Mathf.Max(0.1f, minimumTime),
            Mathf.Max(0.1f, maximumTime)
        );
    }
}
