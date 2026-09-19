using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D, IDamageable
{
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void ManaChangedEventHandler(int current, int maximum);
    [Signal] public delegate void ExperienceChangedEventHandler(long current, long maximum);
    [Signal] public delegate void ProgressionChangedEventHandler(int level, long reset);
    [Signal] public delegate void XpGainedEventHandler(long amount, long totalXp);
    [Signal] public delegate void LevelUpEventHandler(int level, long reset);
    [Signal] public delegate void ResetCompletedEventHandler(long reset);
    [Signal] public delegate void BattlePowerChangedEventHandler(long baseBattlePower);
    [Signal]
    public delegate void CombatStatsChangedEventHandler(
        string characterName,
        long attack,
        long defense,
        long kiAttack);
    [Signal] public delegate void DebugCommandResultEventHandler(string message, bool success);
    [Signal]
    public delegate void DamageFeedbackRequestedEventHandler(
        long amount,
        Vector2 worldPosition,
        bool isReceivedDamage);

    private enum PlayerState
    {
        Idle,
        Walk,
        Attack,
        Dead,
    }

    private const decimal BasicPhysicalAttackMultiplier = 1.0m;
    private const float MinimumFacingDot = 0.15f;
    private const ulong RejectionLogIntervalMsec = 1000;
    private const ulong TravelCooldownMsec = 500;
    private const ulong DebugCommandCooldownMsec = 250;
    private static readonly Vector2 DamageNumberOffset = new(0.0f, -96.0f);

    // Preserve the existing movement limits independently of transparent sprite padding.
    private static readonly Rect2 MovementViewportBounds = new(-64, -69, 128, 138);
    private static readonly Rect2 AttackViewportBounds = new(-64, -62, 128, 142);
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");
    private static readonly StringName AttackAnimation = new("attack");
    private static readonly ExponentialXpCurve XpCurve = new(XpCurveSettings.Default);
    private static readonly CharacterProgression Progression = new(XpCurve);
    private static readonly BattlePowerProgression BattlePowerProgression =
        new(BattlePowerSettings.Default);
    private static readonly CombatStatsCalculator CombatStatsCalculator = new();
    private static readonly IPhysicalDamageCalculator PhysicalDamageCalculator =
        new PhysicalDamageCalculator();

    [Export] public float MoveSpeed { get; set; } = 200.0f;
    [Export] public int OwnerPeerId { get; set; }
    [Export] public string AuthenticatedUserId { get; set; } = string.Empty;
    [Export] public string CharacterId { get; set; } = string.Empty;
    [Export] public string CharacterName { get; set; } = string.Empty;
    [Export]
    public long TotalXp
    {
        get => _totalXp;
        set { _totalXp = value; RefreshProgressionPresentation(); }
    }
    [Export]
    public int Level
    {
        get => _level;
        set { _level = value; RefreshProgressionPresentation(); }
    }
    [Export]
    public long Reset
    {
        get => _reset;
        set { _reset = value; RefreshProgressionPresentation(); }
    }
    [Export]
    public long BaseBattlePower
    {
        get => _baseBattlePower;
        set
        {
            if (_baseBattlePower == value)
                return;

            _baseBattlePower = value;
            RefreshBattlePowerPresentation();
            RecalculateCombatStats();
        }
    }
    [Export]
    public string ActiveCharacterId
    {
        get => _activeCharacterId;
        set
        {
            CharacterDefinition definition = CharacterRegistry.ResolveOrDefault(
                value,
                out bool usedFallback);
            if (usedFallback)
            {
                GD.PushError(
                    $"[CHARACTER] Id ativo inválido '{value}'; usando "
                    + $"'{CharacterRegistry.DefaultCharacterId}'.");
            }

            if (_activeCharacterId == definition.Id)
                return;

            _activeCharacterId = definition.Id;
            RecalculateCombatStats();
        }
    }
    [Export]
    public string MapId
    {
        get => _mapId;
        set
        {
            _mapId = value;
            UpdateLocalCameraLimits();
        }
    }
    [Export] public float AttackRange { get; set; } = 80.0f;
    [Export] public float AttackCooldown { get; set; } = 0.5f;
    [Export] public float AttackActionDuration { get; set; } = 0.6f;
    [Export] public float AttackOffsetX { get; set; } = 46.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int MaxMana { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 3.0f;
    [Export] public PackedScene? FloatingDamageNumberScene { get; set; }

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
    public long CurrentExperience => Progression.FromTotalXp(TotalXp).XpIntoLevel;
    public long MaxExperience => Progression.FromTotalXp(TotalXp).XpRequiredForNextLevel;
    public CombatStats CurrentCombatStats => _combatStats;
    public long Defense => CurrentCombatStats.Defense;
    public CharacterDefinition ActiveCharacterDefinition => CharacterRegistry.Get(ActiveCharacterId);
    public bool IsDead => _health?.IsDead ?? false;
    public bool IsRespawning => _health?.IsRespawning ?? false;
    public bool CanAct => _health?.CanAct ?? false;
    public HealthComponent Health =>
        _health ?? throw new InvalidOperationException($"Health ausente em {GetPath()}.");

    private AnimatedSprite2D? _animatedSprite;
    private CollisionShape2D? _bodyShape;
    private Area2D? _attackArea;
    private CollisionShape2D? _attackShape;
    private HealthComponent? _health;
    private NetworkInterpolation2D? _interpolation;
    private Camera2D? _localCamera;
    private string _mapId = WorldMaps.KameHouse;
    private long _totalXp;
    private int _level;
    private long _reset;
    private long _baseBattlePower = BattlePowerSettings.Default.InitialBattlePower;
    private string _activeCharacterId = CharacterRegistry.DefaultCharacterId;
    private CombatStats _combatStats;
    private bool _progressionReady;
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
    private ulong _nextTravelAllowedMsec;
    private ulong _nextDebugCommandAllowedMsec;

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

        CurrentMana = Mathf.Max(MaxMana, 0);

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
        _spawnPosition = GlobalPosition;
        _progressionReady = true;
        EmitSignal(SignalName.ManaChanged, CurrentMana, MaxMana);
        RefreshProgressionPresentation();
        RefreshBattlePowerPresentation();
        RecalculateCombatStats();

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

        if (!_health.ApplyDamage(damageInfo))
            return false;

        SendDamageFeedback(
            damageInfo.Amount,
            GlobalPosition + DamageNumberOffset,
            isReceivedDamage: true);
        return true;
    }

    public void ApplyAuthenticatedInitialState(AuthenticatedCharacterData data)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer())
        {
            GD.PushWarning("[AUTH] Estado autenticado bloqueado fora do servidor.");
            return;
        }

        if (_health is null)
        {
            GD.PushError($"[AUTH] HealthComponent ausente ao inicializar {GetPath()}.");
            return;
        }

        AuthenticatedUserId = data.UserId.ToString("D");
        CharacterId = data.CharacterId.ToString("D");
        CharacterName = data.CharacterName;
        ProgressionSnapshot progression = RebuildProgression(data.TotalXp);
        TotalXp = progression.State.TotalXp;
        Level = progression.State.Level;
        Reset = progression.State.Reset;
        BaseBattlePower = data.BaseBattlePower;
        ActiveCharacterId = data.ActiveCharacterId;
        MapId = WorldMaps.TryGetBounds(data.MapId, out _)
            ? data.MapId : WorldMaps.KameHouse;
        MaxHealth = Mathf.Max(data.MaxHealth, 1);
        _health.Configure(CharacterName, MaxHealth, RespawnDelay);
        _health.CurrentHealth = Mathf.Clamp(data.CurrentHealth, 0, MaxHealth);
        ClampToViewport();
        _spawnPosition = GlobalPosition;

        if (_health.CurrentHealth == 0)
            _health.Kill();
    }

    public bool TryCaptureAuthenticatedState(out AuthenticatedPlayerState? state)
    {
        state = null;
        if (!Guid.TryParse(AuthenticatedUserId, out Guid userId)
            || !Guid.TryParse(CharacterId, out Guid characterId)
            || !float.IsFinite(GlobalPosition.X)
            || !float.IsFinite(GlobalPosition.Y))
        {
            return false;
        }

        state = new AuthenticatedPlayerState(
            userId,
            characterId,
            CurrentHealth,
            Level,
            Reset,
            TotalXp,
            BaseBattlePower,
            ActiveCharacterId,
            MapId,
            GlobalPosition.X,
            GlobalPosition.Y);
        return true;
    }

    public static ProgressionSnapshot RebuildProgression(long totalXp) =>
        Progression.FromTotalXp(totalXp);

    public static long GetGlobalLevel(long reset, int level) =>
        XpCurve.GetGlobalLevel(reset, level);

    public bool AddXp(long amount)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer())
            return false;
        if (amount < 0)
            return false;

        ProgressionResult result;
        long updatedBattlePower;
        try
        {
            result = Progression.AddXp(new(TotalXp, Level, Reset), amount);
            updatedBattlePower = BattlePowerProgression.AddCompletedLevels(
                BaseBattlePower,
                result.Steps.Count);
        }
        catch (OverflowException)
        {
            GD.PushWarning(
                $"[XP] Concessão excederia o limite numérico de progressão de {CharacterId}.");
            return false;
        }
        catch (ArgumentException exception)
        {
            GD.PushError($"[XP] Estado inválido para {CharacterId}: {exception.Message}");
            return false;
        }

        if (amount == 0)
            return true;

        TotalXp = result.Snapshot.State.TotalXp;
        Level = result.Snapshot.State.Level;
        Reset = result.Snapshot.State.Reset;
        if (result.Steps.Count > 0)
        {
            BaseBattlePower = updatedBattlePower;
            EmitSignal(SignalName.BattlePowerChanged, BaseBattlePower);
        }
        EmitSignal(SignalName.XpGained, amount, TotalXp);
        foreach (ProgressionStep step in result.Steps)
        {
            EmitSignal(SignalName.LevelUp, step.Level, step.Reset);
            if (step.ResetCompleted)
                EmitSignal(SignalName.ResetCompleted, step.Reset);
        }

        return true;
    }

    private void RefreshProgressionPresentation()
    {
        if (!_progressionReady || NetworkManager.RunningAsServer
            || OwnerPeerId != Multiplayer.GetUniqueId() || TotalXp < 0)
            return;

        ProgressionSnapshot snapshot = RebuildProgression(TotalXp);
        EmitSignal(SignalName.ExperienceChanged,
            snapshot.XpIntoLevel, snapshot.XpRequiredForNextLevel);
        EmitSignal(SignalName.ProgressionChanged, Level, Reset);
    }

    private void RefreshBattlePowerPresentation()
    {
        if (!_progressionReady || NetworkManager.RunningAsServer
            || OwnerPeerId != Multiplayer.GetUniqueId())
            return;

        EmitSignal(SignalName.BattlePowerChanged, BaseBattlePower);
    }

    private void RecalculateCombatStats()
    {
        CharacterDefinition definition = CharacterRegistry.Get(ActiveCharacterId);
        _combatStats = CombatStatsCalculator.Calculate(BaseBattlePower, definition);

        if (!_progressionReady || NetworkManager.RunningAsServer
            || OwnerPeerId != Multiplayer.GetUniqueId())
        {
            return;
        }

        EmitSignal(
            SignalName.CombatStatsChanged,
            definition.Name,
            _combatStats.Attack,
            _combatStats.Defense,
            _combatStats.KiAttack);
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
        Vector2 direction = GetViewport().GuiGetFocusOwner() is LineEdit
            ? Vector2.Zero
            : Input.GetVector("move_left", "move_right", "move_up", "move_down");
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
        if (GetViewport().GuiGetFocusOwner() is not LineEdit
            && Input.IsActionJustPressed("attack"))
            RpcId(NetworkConstants.ServerPeerId, MethodName.RequestAttack);
    }

    public void SubmitDebugCommand(string command)
    {
        if (NetworkManager.RunningAsServer || OwnerPeerId != Multiplayer.GetUniqueId())
            return;

        if (string.IsNullOrWhiteSpace(command)
            || command.Length > DebugXpCommands.MaximumCommandLength)
        {
            EmitSignal(SignalName.DebugCommandResult,
                "Comando vazio ou grande demais.", false);
            return;
        }

        RpcId(NetworkConstants.ServerPeerId, MethodName.RequestDebugCommand, command);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RequestDebugCommand(string command)
    {
        if (!TryValidateSender(out int senderId, "comando de debug"))
            return;

        ulong now = Time.GetTicksMsec();
        if (now < _nextDebugCommandAllowedMsec)
            return;
        _nextDebugCommandAllowedMsec = now + DebugCommandCooldownMsec;

        if (string.IsNullOrWhiteSpace(CharacterId))
        {
            SendDebugCommandResult(senderId,
                new(false, "Personagem autenticado não encontrado."));
            return;
        }

        long previousTotalXp = TotalXp;
        int previousLevel = Level;
        long previousReset = Reset;
        DebugXpCommandResult result = DebugXpCommands.Execute(
            this,
            command,
            DebugXpCommands.ServerCommandsEnabled);

        if (result.Success && result.GrantedXp > 0)
        {
            GD.Print(
                $"[DEBUG XP] Personagem {CharacterId} recebeu {result.GrantedXp} XP por comando.");
            GD.Print(
                $"[DEBUG XP] Reset {previousReset} / Level {previousLevel} -> "
                + $"Reset {Reset} / Level {Level}; TotalXp {previousTotalXp} -> {TotalXp}.");
        }
        else if (result.Success)
        {
            GD.Print($"[DEBUG XP] Personagem {CharacterId}: {result.Message}");
        }

        SendDebugCommandResult(senderId, result);
    }

    private void SendDebugCommandResult(int peerId, DebugXpCommandResult result)
    {
        RpcId(peerId, MethodName.ReceiveDebugCommandResult, result.Message, result.Success);
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void ReceiveDebugCommandResult(string message, bool success)
    {
        if (NetworkManager.RunningAsServer || OwnerPeerId != Multiplayer.GetUniqueId())
            return;

        EmitSignal(SignalName.DebugCommandResult, message, success);
    }

    public void TravelToMap(string mapId)
    {
        if (NetworkManager.RunningAsServer || OwnerPeerId != Multiplayer.GetUniqueId())
            return;

        RpcId(NetworkConstants.ServerPeerId, MethodName.RequestTravelToMap, mapId);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RequestTravelToMap(string mapId)
    {
        if (!TryValidateSender(out int senderId, "viagem") || !CanAct)
            return;

        if (mapId is null || mapId.Length > 32
            || !WorldMaps.TryGetBounds(mapId, out _) || mapId == MapId)
        {
            GD.PushWarning($"[SERVER] Destino inválido para o peer {senderId}.");
            return;
        }

        ulong now = Time.GetTicksMsec();
        if (now < _nextTravelAllowedMsec)
            return;
        _nextTravelAllowedMsec = now + TravelCooldownMsec;

        _serverInputDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        IsAttacking = false;
        _attackActionRemaining = 0.0f;
        DisableAttackArea();
        MapId = mapId;
        GlobalPosition = WorldMaps.GetArrivalPosition(mapId);
        _spawnPosition = GlobalPosition;
        GD.Print($"[SERVER] Peer {senderId} viajou para {mapId}.");
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
            || !GetMapBounds().HasPoint(GlobalPosition)
        )
        {
            LogAttackRejection(senderId, "posição oficial inválida");
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

        int hitCount = ApplyDamageToNpcsInRange();
        if (hitCount == 0)
            GD.Print($"[SERVER][COMBAT] Peer {senderId}: nenhum NPC válido no alcance.");
    }

    private int ApplyDamageToNpcsInRange()
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

            if (TryApplyServerPhysicalAttack(
                    npc,
                    BasicPhysicalAttackMultiplier,
                    out _))
                hitCount++;
        }

        return hitCount;
    }

    internal bool TryApplyServerPhysicalAttack(
        NpcBase target,
        decimal attackMultiplier,
        out long damage)
    {
        damage = 0;
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer()
            || !CanAct || target is null || !target.CanAct)
        {
            return false;
        }

        long attack = CurrentCombatStats.Attack;
        try
        {
            damage = PhysicalDamageCalculator.Calculate(
                attack,
                target.Defense,
                attackMultiplier);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            GD.PushError(
                $"[SERVER][COMBAT] Stats físicos inválidos no ataque de {CharacterId}: "
                + exception.Message);
            return false;
        }

#if DEBUG
        GD.Print(
            $"[COMBAT] Atacante {CharacterId} | Attack: {attack} | "
            + $"Defense do alvo: {target.Defense} | Multiplicador: {attackMultiplier} | "
            + $"Dano: {damage}");
#endif

        DamageInfo damageInfo = new(
            DamageSourceType.Player,
            $"peer {OwnerPeerId}",
            damage,
            GlobalPosition,
            OwnerPeerId);
        if (!target.ApplyServerDamage(damageInfo))
            return false;

        SendDamageFeedback(
            damage,
            target.GlobalPosition + DamageNumberOffset,
            isReceivedDamage: false);
        return true;
    }

    private void SendDamageFeedback(long amount, Vector2 worldPosition, bool isReceivedDamage)
    {
        if (!NetworkManager.RunningAsServer || !Multiplayer.IsServer()
            || amount <= 0 || !IsFinite(worldPosition))
        {
            return;
        }

        EmitSignal(
            SignalName.DamageFeedbackRequested,
            amount,
            worldPosition,
            isReceivedDamage);

        if (!IsPeerConnected(OwnerPeerId))
            return;

        RpcId(
            OwnerPeerId,
            MethodName.ReceiveDamageFeedback,
            amount,
            worldPosition,
            isReceivedDamage);
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void ReceiveDamageFeedback(
        long amount,
        Vector2 worldPosition,
        bool isReceivedDamage)
    {
        if (NetworkManager.RunningAsServer
            || OwnerPeerId != Multiplayer.GetUniqueId()
            || amount <= 0
            || !IsFinite(worldPosition))
        {
            return;
        }

        ShowLocalDamageNumber(amount, worldPosition, isReceivedDamage);
    }

    internal bool ShowLocalDamageNumber(
        long amount,
        Vector2 worldPosition,
        bool isReceivedDamage)
    {
        if (amount <= 0 || !IsFinite(worldPosition) || FloatingDamageNumberScene is null)
            return false;

        Node? presentationRoot = GetTree().CurrentScene;
        if (presentationRoot is null)
            return false;

        FloatingDamageNumber number = FloatingDamageNumberScene.Instantiate<FloatingDamageNumber>();
        presentationRoot.AddChild(number);
        number.GlobalPosition = worldPosition;
        number.ShowDamage(amount, isReceivedDamage);
        return true;
    }

    private bool IsPeerConnected(int peerId)
    {
        if (peerId <= NetworkConstants.ServerPeerId || !Multiplayer.HasMultiplayerPeer())
            return false;

        foreach (int connectedPeerId in Multiplayer.GetPeers())
        {
            if (connectedPeerId == peerId)
                return true;
        }

        return false;
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
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
        _localCamera = new Camera2D
        {
            Name = "LocalCamera",
            Enabled = true,
            PositionSmoothingEnabled = false,
        };
        UpdateLocalCameraLimits();
        AddChild(_localCamera);
        _localCamera.MakeCurrent();
    }

    private void UpdateLocalCameraLimits()
    {
        if (_localCamera is null)
            return;

        Rect2 bounds = GetMapBounds();
        _localCamera.LimitLeft = Mathf.RoundToInt(bounds.Position.X);
        _localCamera.LimitTop = Mathf.RoundToInt(bounds.Position.Y);
        _localCamera.LimitRight = Mathf.RoundToInt(bounds.End.X);
        _localCamera.LimitBottom = Mathf.RoundToInt(bounds.End.Y);
    }

    private void ApplyVisualState()
    {
        if (_animatedSprite is null || IsDead || IsRespawning)
            return;

        UpdateSpriteDirection();

        if (IsAttacking)
        {
            _currentState = PlayerState.Attack;
            if (_animatedSprite.Animation != AttackAnimation || !_animatedSprite.IsPlaying())
                _animatedSprite.Play(AttackAnimation);
            return;
        }

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

        UpdateMovementPresentation(Velocity);
    }

    private void ClampToViewport()
    {
        Rect2 viewportRect = GetMapBounds();
        if (_animatedSprite is null)
        {
            GlobalPosition = GlobalPosition.Clamp(viewportRect.Position, viewportRect.End);
            return;
        }

        Rect2 bounds = IsAttacking ? AttackViewportBounds : MovementViewportBounds;
        Vector2 minimumPosition = viewportRect.Position - bounds.Position;
        Vector2 maximumPosition = viewportRect.End - bounds.End;
        GlobalPosition = GlobalPosition.Clamp(minimumPosition, maximumPosition);
    }

    private Rect2 GetMapBounds() =>
        WorldMaps.TryGetBounds(MapId, out Rect2 bounds) ? bounds : WorldMaps.KameHouseBounds;
}
