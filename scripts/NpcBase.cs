using Godot;
using System;

public partial class NpcBase : CharacterBody2D, IDamageable
{
    public const int IdleAiState = 0;
    public const int MovingAiState = 1;

    private static readonly StringName AttackAnimation = new("attack");

    [Export] public float MoveSpeed { get; set; } = 40.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 5.0f;
    [Export] public Vector2 MovementDirection { get; set; } = Vector2.Zero;
    [Export(PropertyHint.Enum, "Idle,Moving")] public int AiState { get; set; } = IdleAiState;

    [Export]
    public bool IsAttacking
    {
        get => _isAttacking;
        set => _isAttacking = value;
    }

    public bool NetworkSetupIsValid { get; private set; }
    public bool IsDead => _health?.IsDead ?? false;
    public bool IsRespawning => _health?.IsRespawning ?? false;
    public bool CanAct => _health?.CanAct ?? false;
    public HealthComponent Health =>
        _health ?? throw new InvalidOperationException($"Health ausente em {GetPath()}.");

    protected Sprite2D? Sprite;
    protected AnimatedSprite2D? AnimatedSprite;

    private CollisionShape2D? _bodyShape;
    private Area2D? _hurtbox;
    private CollisionShape2D? _hurtboxShape;
    private ProgressBar? _healthBar;
    private HealthComponent? _health;
    private NetworkInterpolation2D? _interpolation;
    private Vector2 _spawnPosition;
    private bool _isAttacking;
    private bool _clientSynchronizationLogged;
    private double _clientSynchronizationDelay;
    private bool _invalidAuthorityLogged;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(NetworkConstants.ServerPeerId);
    }

    public override void _Ready()
    {
        Sprite = GetNodeOrNull<Sprite2D>("VisualRoot/Sprite2D");
        AnimatedSprite = GetNodeOrNull<AnimatedSprite2D>("VisualRoot/AnimatedSprite2D");
        _bodyShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
        _hurtboxShape = GetNodeOrNull<CollisionShape2D>("Hurtbox/CollisionShape2D");
        _healthBar = GetNodeOrNull<ProgressBar>("VisualRoot/HealthBar");
        _health = GetNodeOrNull<HealthComponent>("Health");
        _interpolation = GetNodeOrNull<NetworkInterpolation2D>("NetworkInterpolation");

        _spawnPosition = GlobalPosition;

        if (_health is null)
        {
            GD.PushError($"[NPC] HealthComponent ausente: {GetPath()}");
        }
        else
        {
            _health.HealthChanged += OnHealthChanged;
            _health.StateChanged += OnHealthStateChanged;
            _health.Died += OnDiedInternal;
            _health.RespawnReady += OnRespawnReady;
            _health.Configure(Name, MaxHealth, RespawnDelay);
        }

        ApplyHealthState();
        NetworkSetupIsValid = ValidateNetworkSetup();

        if (NetworkManager.RunningAsServer)
            GD.Print($"[SERVER] NPC inicializado: {Name}");
    }

    public override void _ExitTree()
    {
        _interpolation?.PrepareForRemoval();

        if (_health is null)
            return;

        _health.HealthChanged -= OnHealthChanged;
        _health.StateChanged -= OnHealthStateChanged;
        _health.Died -= OnDiedInternal;
        _health.RespawnReady -= OnRespawnReady;
    }

    public bool ApplyServerDamage(DamageInfo damageInfo)
    {
        if (!CanRunServerAi() || _health is null)
            return false;

        return _health.ApplyDamage(damageInfo);
    }

    protected bool CanRunServerAi()
    {
        if (!NetworkManager.RunningAsServer)
            return false;

        if (Multiplayer.IsServer())
            return true;

        if (!_invalidAuthorityLogged)
        {
            _invalidAuthorityLogged = true;
            GD.PushError($"[NPC] IA bloqueada fora do servidor: {GetPath()}");
        }

        return false;
    }

    protected void UpdateServerMovementState(Vector2 direction)
    {
        MovementDirection = direction.LimitLength(1.0f);
        AiState = Velocity.LengthSquared() > 0.0001f ? MovingAiState : IdleAiState;
        UpdateSpriteDirection();
    }

    protected void UpdateClientPresentation()
    {
        ApplyHealthState();
        UpdateSpriteDirection();

        if (
            !_clientSynchronizationLogged
            && Multiplayer.HasMultiplayerPeer()
            && Multiplayer.GetUniqueId() != NetworkConstants.ServerPeerId
        )
        {
            _clientSynchronizationDelay += GetPhysicsProcessDeltaTime();
            if (_clientSynchronizationDelay < 0.5)
                return;

            _clientSynchronizationLogged = true;
            GD.Print(
                $"[CLIENT] NPC sincronizado: {Name} | posição: {GlobalPosition} "
                + $"| direção: {MovementDirection} | estado: {AiState} "
                + $"| vida: {Health.CurrentHealth}/{Health.MaxHealth} | morto: {IsDead}"
            );
        }
    }

    protected void LogServerAiActive()
    {
        GD.Print($"[SERVER] IA ativa para: {Name}");
    }

    protected void SetServerAttacking(bool isAttacking)
    {
        if (!CanRunServerAi())
            return;

        IsAttacking = isAttacking && CanAct;
    }

    protected virtual void OnDied()
    {
    }

    protected virtual void OnRespawned()
    {
    }

    protected Vector2 KeepInsideViewport()
    {
        Rect2 viewportRect = GetViewportRect();
        Vector2 halfSpriteSize;

        if (Sprite is not null)
        {
            halfSpriteSize = Sprite.GetRect().Size * Sprite.Scale.Abs() / 2.0f;
        }
        else if (AnimatedSprite is not null)
        {
            Texture2D? texture = AnimatedSprite.SpriteFrames.GetFrameTexture(
                AnimatedSprite.Animation,
                AnimatedSprite.Frame
            );
            halfSpriteSize = texture?.GetSize() * AnimatedSprite.Scale.Abs() / 2.0f
                ?? Vector2.Zero;
        }
        else
        {
            halfSpriteSize = Vector2.Zero;
        }

        Vector2 minimumPosition = viewportRect.Position + halfSpriteSize;
        Vector2 maximumPosition = viewportRect.End - halfSpriteSize;
        Vector2 inwardDirection = Vector2.Zero;

        if (GlobalPosition.X < minimumPosition.X)
            inwardDirection.X = 1.0f;
        else if (GlobalPosition.X > maximumPosition.X)
            inwardDirection.X = -1.0f;

        if (GlobalPosition.Y < minimumPosition.Y)
            inwardDirection.Y = 1.0f;
        else if (GlobalPosition.Y > maximumPosition.Y)
            inwardDirection.Y = -1.0f;

        GlobalPosition = GlobalPosition.Clamp(minimumPosition, maximumPosition);
        return inwardDirection;
    }

    protected void UpdateSpriteDirection()
    {
        if (MovementDirection.X > 0.0f)
        {
            if (Sprite is not null)
                Sprite.FlipH = false;
            if (AnimatedSprite is not null)
                AnimatedSprite.FlipH = false;
        }
        else if (MovementDirection.X < 0.0f)
        {
            if (Sprite is not null)
                Sprite.FlipH = true;
            if (AnimatedSprite is not null)
                AnimatedSprite.FlipH = true;
        }
    }

    private void OnHealthChanged(int current, int maximum)
    {
        if (_healthBar is null)
            return;

        _healthBar.MaxValue = maximum;
        _healthBar.Value = current;
    }

    private void OnHealthStateChanged(bool isDead, bool isRespawning)
    {
        ApplyHealthState();
    }

    private void OnDiedInternal()
    {
        MovementDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        AiState = IdleAiState;
        IsAttacking = false;
        OnDied();
    }

    private void OnRespawnReady()
    {
        if (!CanRunServerAi() || _health is null)
            return;

        GlobalPosition = _spawnPosition;
        MovementDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        AiState = IdleAiState;
        IsAttacking = false;
        OnRespawned();
        _health.CompleteRespawn();
    }

    private void ApplyHealthState()
    {
        bool unavailable = IsDead || IsRespawning;
        if (unavailable)
        {
            Velocity = Vector2.Zero;
            MovementDirection = Vector2.Zero;
            AiState = IdleAiState;
            IsAttacking = false;
        }

        _bodyShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, unavailable);
        _hurtboxShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, unavailable);
        _hurtbox?.SetDeferred(Area2D.PropertyName.Monitoring, !unavailable);
        _hurtbox?.SetDeferred(Area2D.PropertyName.Monitorable, !unavailable);
        _interpolation?.SetSuspended(unavailable, "respawn");
        Visible = !unavailable;
    }

    private bool ValidateNetworkSetup()
    {
        bool isValid = true;

        if (_bodyShape is null)
        {
            GD.PushError($"[NPC] CollisionShape2D não encontrado: {GetPath()}");
            isValid = false;
        }

        if (_hurtbox is null || _hurtboxShape is null)
        {
            GD.PushError($"[NPC] Hurtbox não encontrada: {GetPath()}");
            isValid = false;
        }

        if (_health is null)
        {
            GD.PushError($"[NPC] HealthComponent não encontrado: {GetPath()}");
            isValid = false;
        }

        if (!NetworkManager.RunningAsServer && _interpolation is null)
        {
            GD.PushError($"[CLIENT][INTERPOLATION] Componente ausente: {GetPath()}");
            isValid = false;
        }

        if (Sprite is null && AnimatedSprite is null)
            GD.PushWarning($"[NPC] Node visual não encontrado; headless continuará ativo: {GetPath()}");

        isValid &= ValidateSynchronizer(
            GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer"),
            new[]
            {
                new NodePath(".:position"),
                new NodePath(".:velocity"),
                new NodePath(".:MovementDirection"),
                new NodePath(".:AiState"),
                new NodePath(".:IsAttacking"),
            },
            GetPath().ToString()
        );

        isValid &= ValidateSynchronizer(
            GetNodeOrNull<MultiplayerSynchronizer>("Health/MultiplayerSynchronizer"),
            new[]
            {
                new NodePath(".:CurrentHealth"),
                new NodePath(".:IsDead"),
                new NodePath(".:IsRespawning"),
            },
            $"{GetPath()}/Health"
        );

        return isValid;
    }

    private static bool ValidateSynchronizer(
        MultiplayerSynchronizer? synchronizer,
        NodePath[] expectedProperties,
        string ownerPath
    )
    {
        if (synchronizer is null)
        {
            GD.PushError($"[NETWORK] MultiplayerSynchronizer não encontrado: {ownerPath}");
            return false;
        }

        bool isValid = true;
        if (synchronizer.RootPath != new NodePath(".."))
        {
            GD.PushError($"[NETWORK] RootPath inválido no MultiplayerSynchronizer: {ownerPath}");
            isValid = false;
        }

        SceneReplicationConfig? replicationConfig = synchronizer.ReplicationConfig;
        if (replicationConfig is null)
        {
            GD.PushError($"[NETWORK] SceneReplicationConfig ausente: {ownerPath}");
            return false;
        }

        Godot.Collections.Array<NodePath> configuredProperties = replicationConfig.GetProperties();
        foreach (NodePath expectedProperty in expectedProperties)
        {
            if (configuredProperties.Contains(expectedProperty))
                continue;

            GD.PushError(
                $"[NETWORK] Propriedade de replicação ausente ({expectedProperty}): {ownerPath}"
            );
            isValid = false;
        }

        return isValid;
    }
}
