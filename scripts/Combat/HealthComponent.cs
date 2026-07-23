using Godot;

public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(int current, int maximum);
    [Signal] public delegate void StateChangedEventHandler(bool isDead, bool isRespawning);
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void RespawnReadyEventHandler();

    private const int MaximumAcceptedDamage = 1000;

    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 5.0f;

    [Export]
    public int CurrentHealth
    {
        get => _currentHealth;
        set
        {
            int clampedValue = Mathf.Clamp(value, 0, Mathf.Max(MaxHealth, 0));
            if (_currentHealth == clampedValue)
                return;

            _currentHealth = clampedValue;
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        }
    }

    [Export]
    public bool IsDead
    {
        get => _isDead;
        set
        {
            if (_isDead == value)
                return;

            _isDead = value;
            EmitStateChanged();
        }
    }

    [Export]
    public bool IsRespawning
    {
        get => _isRespawning;
        set
        {
            if (_isRespawning == value)
                return;

            _isRespawning = value;
            EmitStateChanged();
        }
    }

    public bool CanAct => !IsDead && !IsRespawning && CurrentHealth > 0;

    private int _currentHealth = 100;
    private bool _isDead;
    private bool _isRespawning;
    private Timer? _respawnTimer;
    private string _entityName = "Entidade";
    private bool _serverInitialized;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(NetworkConstants.ServerPeerId);
    }

    public override void _Ready()
    {
        _respawnTimer = GetNodeOrNull<Timer>("RespawnTimer");
        if (_respawnTimer is null)
        {
            GD.PushError($"[HEALTH] RespawnTimer ausente: {GetPath()}");
            return;
        }

        _respawnTimer.OneShot = true;
        _respawnTimer.Timeout += OnRespawnTimerTimeout;
    }

    public override void _ExitTree()
    {
        if (_respawnTimer is not null)
        {
            _respawnTimer.Stop();
            _respawnTimer.Timeout -= OnRespawnTimerTimeout;
        }
    }

    public void Configure(string entityName, int maxHealth, float respawnDelay)
    {
        _entityName = string.IsNullOrWhiteSpace(entityName) ? GetParent().Name : entityName;
        MaxHealth = Mathf.Max(maxHealth, 1);
        RespawnDelay = Mathf.Max(respawnDelay, 0.01f);

        if (IsServerAuthority() && !_serverInitialized)
        {
            _serverInitialized = true;
            CurrentHealth = MaxHealth;
            IsDead = false;
            IsRespawning = false;
        }

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        EmitStateChanged();
    }

    public bool ApplyDamage(DamageInfo damageInfo)
    {
        if (!IsServerAuthority())
        {
            GD.PushWarning($"[HEALTH] Dano bloqueado fora do servidor: {_entityName}");
            return false;
        }

        if (damageInfo.Amount <= 0 || damageInfo.Amount > MaximumAcceptedDamage)
        {
            GD.PushWarning(
                $"[SERVER][COMBAT] Dano inválido rejeitado para {_entityName}: {damageInfo.Amount}"
            );
            return false;
        }

        if (!CanAct)
            return false;

        CurrentHealth = Mathf.Max(CurrentHealth - damageInfo.Amount, 0);
        GD.Print(
            $"[SERVER][COMBAT] {_entityName} recebeu {damageInfo.Amount} de dano "
            + $"de {damageInfo.AttackerId}."
        );
        GD.Print($"[SERVER][HEALTH] {_entityName}: {CurrentHealth}/{MaxHealth}");

        if (CurrentHealth == 0)
            Kill();

        return true;
    }

    public bool Heal(int amount)
    {
        if (!IsServerAuthority() || !CanAct || amount <= 0)
            return false;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        return true;
    }

    public void Kill()
    {
        if (!IsServerAuthority() || IsDead || IsRespawning)
            return;

        CurrentHealth = 0;
        IsDead = true;
        IsRespawning = true;
        GD.Print($"[SERVER][DEATH] {_entityName} morreu");
        GD.Print(
            $"[SERVER][RESPAWN] {_entityName} respawnará em {RespawnDelay:0.##} segundos"
        );
        EmitSignal(SignalName.Died);

        if (_respawnTimer is null)
        {
            GD.PushError($"[SERVER][RESPAWN] Timer ausente para {_entityName}.");
            return;
        }

        _respawnTimer.Start(RespawnDelay);
    }

    public void CompleteRespawn()
    {
        if (!IsServerAuthority() || !IsDead || !IsRespawning)
        {
            GD.PushWarning($"[SERVER][RESPAWN] Respawn inválido ou duplicado: {_entityName}");
            return;
        }

        CurrentHealth = MaxHealth;
        IsDead = false;
        IsRespawning = false;
        GD.Print($"[SERVER][RESPAWN] {_entityName} respawnado");
    }

    private void OnRespawnTimerTimeout()
    {
        if (!IsInsideTree() || !IsServerAuthority() || !IsDead || !IsRespawning)
            return;

        EmitSignal(SignalName.RespawnReady);
    }

    private bool IsServerAuthority()
    {
        return NetworkManager.RunningAsServer && Multiplayer.IsServer();
    }

    private void EmitStateChanged()
    {
        EmitSignal(SignalName.StateChanged, IsDead, IsRespawning);
    }
}
