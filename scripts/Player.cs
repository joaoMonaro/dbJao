using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D, IDamageable
{
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void ManaChangedEventHandler(int current, int maximum);
    [Signal] public delegate void ExperienceChangedEventHandler(int current, int maximum);

    private enum PlayerState
    {
        Idle,
        Walk,
        Attack,
        Dead,
    }

    private const int MaximumServerDamage = 100;
    private const float MinimumFacingDot = 0.15f;
    private const ulong RejectionLogIntervalMsec = 1000;

    private static readonly Vector2 NormalVisualScale = Vector2.One;
    private static readonly Vector2 AttackVisualScale = new(0.5f, 0.5f);
    private static readonly Vector2 NormalVisualOffset = Vector2.Zero;
    private static readonly Vector2 AttackVisualOffset = new(0.0f, 9.0f);
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");
    private static readonly StringName AttackAnimation = new("attack");

    [Export] public float MoveSpeed { get; set; } = 200.0f;
    [Export] public int OwnerPeerId { get; set; }
    [Export] public int AttackDamage { get; set; } = 20;
    [Export] public float AttackRange { get; set; } = 80.0f;
    [Export] public float AttackCooldown { get; set; } = 0.5f;
    [Export] public float AttackActionDuration { get; set; } = 0.6f;
    [Export] public float AttackOffsetX { get; set; } = 46.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int MaxMana { get; set; } = 100;
    [Export] public int MaxExperience { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 3.0f;

    [Export]
    public Vector2 FacingDirection
    {
        get => _facingDirection;
        set
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
                return;

            Vector2 normalized = value.LimitLength(1.0f);
            if (normalized == Vector2.Zero)
                return;

            _facingDirection = normalized;
            UpdateSpriteDirection();
        }
    }

    [Export]
    public bool IsAttacking
    {
        get => _isAttacking;
        set
        {
            if (_isAttacking == value)
                return;

            _isAttacking = value;
            ApplyVisualState();
        }
    }

    public int CurrentHealth => _health?.CurrentHealth ?? MaxHealth;
    public int CurrentMana { get; private set; }
    public int CurrentExperience { get; private set; }
    public bool IsDead => _health?.IsDead ?? false;
    public bool IsRespawning => _health?.IsRespawning ?? false;
    public bool CanAct => _health?.CanAct ?? false;
    public HealthComponent Health =>
        _health ?? throw new InvalidOperationException($"Health ausente em {GetPath()}.");

    private AnimatedSprite2D? _animatedSprite;
    private CollisionShape2D? _bodyShape;
    private Area2D? _attackArea;
    private CollisionShape2D? _attackShape;
    private ProgressBar? _healthBar;
    private HealthComponent? _health;
    private NetworkInterpolation2D? _interpolation;
    private PlayerState _currentState = PlayerState.Idle;
    private Vector2 _facingDirection = Vector2.Right;
    private bool _isAttacking;
    private Vector2 _spawnPosition;
    private Vector2 _serverInputDirection;
    private Vector2 _lastSentDirection = new(float.NaN, float.NaN);
    private float _inputHeartbeatElapsed;
    private float _attackCooldownRemaining;
    private float _attackActionRemaining;
    private ulong _lastRejectionLogMsec;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(NetworkConstants.ServerPeerId);
    }

    public override void _Ready()
    {
        if (OwnerPeerId <= NetworkConstants.ServerPeerId && int.TryParse(Name, out int peerId))
            OwnerPeerId = peerId;

        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("VisualRoot/AnimatedSprite2D");
        _bodyShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _attackArea = GetNodeOrNull<Area2D>("AttackArea");
        _attackShape = GetNodeOrNull<CollisionShape2D>("AttackArea/CollisionShape2D");
        _healthBar = GetNodeOrNull<ProgressBar>("VisualRoot/HealthBar");
        _health = GetNodeOrNull<HealthComponent>("Health");
        _interpolation = GetNodeOrNull<NetworkInterpolation2D>("NetworkInterpolation");

        if (_bodyShape is null)
            GD.PushError($"[PLAYER] CollisionShape2D ausente: {GetPath()}");
        if (_attackArea is null || _attackShape is null)
            GD.PushError($"[PLAYER] AttackArea/hitbox ausente: {GetPath()}");
        if (_health is null)
        {
            GD.PushError($"[PLAYER] HealthComponent ausente: {GetPath()}");
            return;
        }
        if (!NetworkManager.RunningAsServer && _interpolation is null)
        {
            GD.PushError($"[CLIENT][INTERPOLATION] Componente ausente: {GetPath()}");
        }

        _spawnPosition = GlobalPosition;
        CurrentMana = Mathf.Max(MaxMana, 0);
        CurrentExperience = Mathf.Clamp(CurrentExperience, 0, Mathf.Max(MaxExperience, 0));

        _health.HealthChanged += OnComponentHealthChanged;
        _health.StateChanged += OnHealthStateChanged;
        _health.Died += OnDied;
        _health.RespawnReady += OnRespawnReady;
        _health.Configure($"Jogador {OwnerPeerId}", MaxHealth, RespawnDelay);

        if (_animatedSprite is not null)
            _animatedSprite.AnimationFinished += OnAnimationFinished;

        DisableAttackArea();
        ApplyHealthState();
        ApplyVisualState();
        ClampToViewport();
        EmitSignal(SignalName.ManaChanged, CurrentMana, MaxMana);
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, MaxExperience);

        bool isLocalPlayer =
            !NetworkManager.RunningAsServer && OwnerPeerId == Multiplayer.GetUniqueId();

        if (!NetworkManager.RunningAsServer)
            GD.Print($"[CLIENT] Jogador replicado: {OwnerPeerId} (local: {isLocalPlayer})");

        if (isLocalPlayer)
            CreateLocalCamera();
    }

    public override void _ExitTree()
    {
        _interpolation?.PrepareForRemoval();

        if (_health is not null)
        {
            _health.HealthChanged -= OnComponentHealthChanged;
            _health.StateChanged -= OnHealthStateChanged;
            _health.Died -= OnDied;
            _health.RespawnReady -= OnRespawnReady;
        }

        if (_animatedSprite is not null)
            _animatedSprite.AnimationFinished -= OnAnimationFinished;
    }

    public override void _PhysicsProcess(double delta)
    {
        float frameDelta = (float)delta;

        if (NetworkManager.RunningAsServer)
        {
            ProcessAuthoritativeTimers(frameDelta);
            ProcessAuthoritativeMovement();
            return;
        }

        ApplyVisualState();

        if (!CanAct || OwnerPeerId != Multiplayer.GetUniqueId())
            return;

        CaptureAndSendAttack();
        CaptureAndSendMovement(frameDelta);
    }

    public bool ApplyServerDamage(DamageInfo damageInfo)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer() || _health is null)
            return false;

        return _health.ApplyDamage(damageInfo);
    }

    private void ProcessAuthoritativeTimers(float delta)
    {
        _attackCooldownRemaining = Mathf.Max(_attackCooldownRemaining - delta, 0.0f);

        if (!IsAttacking)
            return;

        _attackActionRemaining = Mathf.Max(_attackActionRemaining - delta, 0.0f);
        if (_attackActionRemaining <= 0.0f)
            IsAttacking = false;
    }

    private void ProcessAuthoritativeMovement()
    {
        if (!CanAct)
        {
            Velocity = Vector2.Zero;
            return;
        }

        if (IsAttacking)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            ClampToViewport();
            return;
        }

        Velocity = _serverInputDirection * MoveSpeed;
        if (Mathf.Abs(_serverInputDirection.X) > 0.001f)
            FacingDirection = _serverInputDirection.X < 0.0f ? Vector2.Left : Vector2.Right;

        UpdateMovementPresentation(Velocity);
        MoveAndSlide();
        ClampToViewport();
    }

    private void CaptureAndSendMovement(float delta)
    {
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        direction = direction.LimitLength(1.0f);

        _inputHeartbeatElapsed += delta;
        bool directionChanged = direction.DistanceSquaredTo(_lastSentDirection) > 0.0001f;
        if (!directionChanged && _inputHeartbeatElapsed < NetworkConstants.InputHeartbeatSeconds)
            return;

        _lastSentDirection = direction;
        _inputHeartbeatElapsed = 0.0f;
        RpcId(NetworkConstants.ServerPeerId, MethodName.SubmitMovementInput, direction);
    }

    private void CaptureAndSendAttack()
    {
        if (Input.IsActionJustPressed("attack"))
            RpcId(NetworkConstants.ServerPeerId, MethodName.RequestAttack);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable
    )]
    private void SubmitMovementInput(Vector2 direction)
    {
        if (!TryValidateSender(out int senderId, "movimento"))
            return;

        if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y))
        {
            GD.PushWarning($"[SERVER] Input inválido rejeitado para o peer {senderId}.");
            return;
        }

        if (!CanAct || IsAttacking)
        {
            _serverInputDirection = Vector2.Zero;
            return;
        }

        _serverInputDirection = direction.LimitLength(1.0f);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RequestAttack()
    {
        if (!TryValidateSender(out int senderId, "ataque"))
            return;

        GD.Print($"[SERVER][COMBAT] Ataque solicitado pelo peer {senderId}");

        if (!CanAct)
        {
            LogAttackRejection(senderId, "jogador morto ou em respawn");
            return;
        }

        if (IsAttacking || _attackCooldownRemaining > 0.0f)
        {
            LogAttackRejection(senderId, "cooldown ativo");
            return;
        }

        if (
            !float.IsFinite(GlobalPosition.X)
            || !float.IsFinite(GlobalPosition.Y)
            || !GetViewportRect().HasPoint(GlobalPosition)
        )
        {
            LogAttackRejection(senderId, "posição oficial inválida");
            return;
        }

        if (AttackDamage <= 0 || AttackDamage > MaximumServerDamage)
        {
            GD.PushError($"[SERVER][COMBAT] Dano configurado inválido: {AttackDamage}");
            return;
        }

        if (AttackRange <= 0.0f || AttackRange > 300.0f)
        {
            GD.PushError($"[SERVER][COMBAT] Alcance configurado inválido: {AttackRange}");
            return;
        }

        StartServerAttack(senderId);
    }

    private bool TryValidateSender(out int senderId, string action)
    {
        senderId = 0;
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer())
        {
            GD.PushWarning($"[SERVER] RPC de {action} recebido fora do servidor.");
            return false;
        }

        senderId = Multiplayer.GetRemoteSenderId();
        if (senderId <= NetworkConstants.ServerPeerId || senderId != OwnerPeerId)
        {
            GD.PushWarning(
                $"[SERVER] {action} rejeitado: peer {senderId} tentou usar jogador {OwnerPeerId}."
            );
            return false;
        }

        bool senderConnected = false;
        foreach (int connectedPeerId in Multiplayer.GetPeers())
        {
            if (connectedPeerId == senderId)
            {
                senderConnected = true;
                break;
            }
        }

        Player? senderPlayer = GetParent()?.GetNodeOrNull<Player>(senderId.ToString());
        if (!senderConnected || senderPlayer != this)
        {
            GD.PushWarning($"[SERVER] {action} rejeitado: jogador do peer {senderId} não existe.");
            return false;
        }

        return true;
    }

    private void StartServerAttack(int senderId)
    {
        _serverInputDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        _attackCooldownRemaining = Mathf.Max(AttackCooldown, 0.05f);
        _attackActionRemaining = Mathf.Max(AttackActionDuration, 0.05f);
        IsAttacking = true;
        GD.Print($"[SERVER][COMBAT] Ataque aceito: peer {senderId}");

        int hitCount = ApplyDamageToNpcsInRange(senderId);
        if (hitCount == 0)
            GD.Print($"[SERVER][COMBAT] Peer {senderId}: nenhum NPC válido no alcance.");
    }

    private int ApplyDamageToNpcsInRange(int senderId)
    {
        Node? npcs = GetParent()?.GetParent()?.GetNodeOrNull("NPCs");
        if (npcs is null)
        {
            GD.PushError("[SERVER][COMBAT] Node NPCs não encontrado durante o ataque.");
            return 0;
        }

        Vector2 officialFacing = FacingDirection.LimitLength(1.0f);
        if (officialFacing == Vector2.Zero)
            officialFacing = Vector2.Right;

        HashSet<ulong> hitInstanceIds = new();
        int hitCount = 0;

        foreach (Node child in npcs.GetChildren())
        {
            if (child is not NpcBase npc || !npc.Health.CanAct)
                continue;

            Vector2 toTarget = npc.GlobalPosition - GlobalPosition;
            float distance = toTarget.Length();
            if (distance > AttackRange)
                continue;

            if (distance > 0.001f && officialFacing.Dot(toTarget / distance) < MinimumFacingDot)
                continue;

            ulong instanceId = npc.GetInstanceId();
            if (!hitInstanceIds.Add(instanceId))
                continue;

            DamageInfo damageInfo = new(
                DamageSourceType.Player,
                $"peer {senderId}",
                AttackDamage,
                GlobalPosition,
                senderId
            );

            if (npc.ApplyServerDamage(damageInfo))
                hitCount++;
        }

        return hitCount;
    }

    private void LogAttackRejection(int senderId, string reason)
    {
        ulong now = Time.GetTicksMsec();
        if (now - _lastRejectionLogMsec < RejectionLogIntervalMsec)
            return;

        _lastRejectionLogMsec = now;
        GD.Print($"[SERVER][COMBAT] Ataque rejeitado do peer {senderId}: {reason}.");
    }

    private void OnComponentHealthChanged(int current, int maximum)
    {
        if (_healthBar is not null)
        {
            _healthBar.MaxValue = maximum;
            _healthBar.Value = current;
        }

        EmitSignal(SignalName.HealthChanged, current, maximum);
    }

    private void OnHealthStateChanged(bool isDead, bool isRespawning)
    {
        ApplyHealthState();
    }

    private void OnDied()
    {
        _serverInputDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        IsAttacking = false;
        _attackActionRemaining = 0.0f;
        DisableAttackArea();
    }

    private void OnRespawnReady()
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer() || _health is null)
            return;

        GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _serverInputDirection = Vector2.Zero;
        _attackActionRemaining = 0.0f;
        _attackCooldownRemaining = 0.0f;
        IsAttacking = false;
        _health.CompleteRespawn();
        ClampToViewport();
    }

    private void ApplyHealthState()
    {
        bool unavailable = IsDead || IsRespawning;
        if (unavailable)
        {
            _currentState = PlayerState.Dead;
            Velocity = Vector2.Zero;
            DisableAttackArea();
        }

        if (_bodyShape is not null)
            _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, unavailable);

        _interpolation?.SetSuspended(unavailable, "respawn");

        Visible = !unavailable;
        if (!unavailable)
        {
            _currentState = PlayerState.Idle;
            ApplyVisualState();
        }
    }

    private void CreateLocalCamera()
    {
        Rect2 viewportRect = GetViewportRect();
        Camera2D camera = new()
        {
            Name = "LocalCamera",
            Enabled = true,
            PositionSmoothingEnabled = false,
            LimitLeft = Mathf.RoundToInt(viewportRect.Position.X),
            LimitTop = Mathf.RoundToInt(viewportRect.Position.Y),
            LimitRight = Mathf.RoundToInt(viewportRect.End.X),
            LimitBottom = Mathf.RoundToInt(viewportRect.End.Y),
        };
        AddChild(camera);
        camera.MakeCurrent();
    }

    private void ApplyVisualState()
    {
        if (_animatedSprite is null || IsDead || IsRespawning)
            return;

        UpdateSpriteDirection();

        if (IsAttacking)
        {
            _currentState = PlayerState.Attack;
            _animatedSprite.Scale = AttackVisualScale;
            _animatedSprite.Position = AttackVisualOffset;
            if (_animatedSprite.Animation != AttackAnimation || !_animatedSprite.IsPlaying())
                _animatedSprite.Play(AttackAnimation);
            return;
        }

        ResetAttackPresentation();
        UpdateMovementPresentation(Velocity);
    }

    private void UpdateMovementPresentation(Vector2 direction)
    {
        if (_animatedSprite is null || IsAttacking || IsDead || IsRespawning)
            return;

        if (direction != Vector2.Zero)
        {
            _currentState = PlayerState.Walk;
            if (_animatedSprite.Animation != WalkAnimation)
                _animatedSprite.Play(WalkAnimation);
        }
        else
        {
            _currentState = PlayerState.Idle;
            if (_animatedSprite.Animation != IdleAnimation)
                _animatedSprite.Play(IdleAnimation);
        }
    }

    private void UpdateSpriteDirection()
    {
        if (_animatedSprite is not null && Mathf.Abs(FacingDirection.X) > 0.001f)
            _animatedSprite.FlipH = FacingDirection.X < 0.0f;

        UpdateAttackAreaPosition();
    }

    private void DisableAttackArea()
    {
        _attackShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _attackArea?.SetDeferred(Area2D.PropertyName.Monitoring, false);
    }

    private void UpdateAttackAreaPosition()
    {
        if (_attackArea is null)
            return;

        Vector2 position = _attackArea.Position;
        position.X = AttackOffsetX * (FacingDirection.X < 0.0f ? -1.0f : 1.0f);
        _attackArea.Position = position;
    }

    private void OnAnimationFinished()
    {
        if (_animatedSprite is null || _animatedSprite.Animation != AttackAnimation || IsAttacking)
            return;

        ResetAttackPresentation();
        UpdateMovementPresentation(Velocity);
    }

    private void ResetAttackPresentation()
    {
        if (_animatedSprite is null)
            return;

        _animatedSprite.Scale = NormalVisualScale;
        _animatedSprite.Position = NormalVisualOffset;
    }

    private void ClampToViewport()
    {
        Rect2 viewportRect = GetViewportRect();
        if (_animatedSprite is null)
        {
            GlobalPosition = GlobalPosition.Clamp(viewportRect.Position, viewportRect.End);
            return;
        }

        Texture2D? currentTexture = _animatedSprite.SpriteFrames.GetFrameTexture(
            _animatedSprite.Animation,
            _animatedSprite.Frame
        );
        if (currentTexture is null)
            return;

        Vector2 halfSpriteSize = currentTexture.GetSize() * _animatedSprite.Scale.Abs() / 2.0f;
        Vector2 visualOffset = _animatedSprite.Position;
        Vector2 minimumPosition = viewportRect.Position + halfSpriteSize - visualOffset;
        Vector2 maximumPosition = viewportRect.End - halfSpriteSize - visualOffset;
        GlobalPosition = GlobalPosition.Clamp(minimumPosition, maximumPosition);
    }
}
