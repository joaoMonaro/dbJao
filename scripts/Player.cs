using Godot;
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
    }

    private static readonly Vector2 NormalVisualScale = Vector2.One;
    private static readonly Vector2 AttackVisualScale = new(0.5f, 0.5f);
    private static readonly Vector2 NormalVisualOffset = Vector2.Zero;
    private static readonly Vector2 AttackVisualOffset = new(0.0f, 9.0f);
    private static readonly StringName IdleAnimation = new("idle");
    private static readonly StringName WalkAnimation = new("walk");
    private static readonly StringName AttackAnimation = new("attack");

    [Export] public float MoveSpeed { get; set; } = 200.0f;
    [Export] public int AttackDamage { get; set; } = 20;
    [Export] public int AttackActiveFrame { get; set; } = 3;
    [Export] public float AttackOffsetX { get; set; } = 46.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int MaxMana { get; set; } = 100;
    [Export] public int MaxExperience { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 5.0f;

    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int CurrentExperience { get; private set; }

    private AnimatedSprite2D _animatedSprite = null!;
    private CollisionShape2D _bodyShape = null!;
    private Area2D _attackArea = null!;
    private CollisionShape2D _attackShape = null!;
    private ProgressBar _healthBar = null!;
    private readonly List<Node> _hitTargets = new();
    private PlayerState _currentState = PlayerState.Idle;
    private float _facingDirection = 1.0f;
    private bool _isDead;
    private Vector2 _spawnPosition;

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _bodyShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _attackArea = GetNode<Area2D>("AttackArea");
        _attackShape = GetNode<CollisionShape2D>("AttackArea/CollisionShape2D");
        _healthBar = GetNode<ProgressBar>("HealthBar");

        _spawnPosition = GlobalPosition;
        CurrentHealth = Mathf.Max(MaxHealth, 0);
        CurrentMana = Mathf.Max(MaxMana, 0);
        CurrentExperience = Mathf.Clamp(CurrentExperience, 0, Mathf.Max(MaxExperience, 0));
        UpdateHealthBar();
        EmitSignal(SignalName.ManaChanged, CurrentMana, MaxMana);
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, MaxExperience);

        _animatedSprite.AnimationFinished += OnAnimationFinished;
        _animatedSprite.FrameChanged += OnAnimatedSpriteFrameChanged;
        _attackArea.AreaEntered += OnAttackAreaAreaEntered;
        DisableAttackArea();
        _animatedSprite.Play(IdleAnimation);
        ClampToViewport();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
            return;

        if (_currentState == PlayerState.Attack)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            ClampToViewport();
            return;
        }

        if (Input.IsActionJustPressed("attack"))
        {
            StartAttack();
            ClampToViewport();
            return;
        }

        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (direction != Vector2.Zero)
            direction = direction.Normalized();

        Velocity = direction * MoveSpeed;
        UpdateMovementState(direction);
        MoveAndSlide();
        ClampToViewport();
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        UpdateHealthBar();

        if (CurrentHealth <= 0)
            Die();
    }

    private void UpdateHealthBar()
    {
        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = CurrentHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    private async void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        Velocity = Vector2.Zero;
        _currentState = PlayerState.Idle;
        DisableAttackArea();
        SetPhysicsProcess(false);
        _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Visible = false;

        await ToSignal(
            GetTree().CreateTimer(Mathf.Max(RespawnDelay, 0.0f)),
            SceneTreeTimer.SignalName.Timeout
        );

        if (IsInsideTree())
            Respawn();
    }

    private void Respawn()
    {
        GlobalPosition = _spawnPosition;
        CurrentHealth = Mathf.Max(MaxHealth, 0);
        UpdateHealthBar();
        _currentState = PlayerState.Idle;
        _hitTargets.Clear();
        _animatedSprite.Scale = NormalVisualScale;
        _animatedSprite.Position = NormalVisualOffset;
        _animatedSprite.Play(IdleAnimation);
        _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        Visible = true;
        _isDead = false;
        SetPhysicsProcess(true);
    }

    private void UpdateMovementState(Vector2 direction)
    {
        if (direction.X > 0.0f)
        {
            _facingDirection = 1.0f;
            _animatedSprite.FlipH = false;
        }
        else if (direction.X < 0.0f)
        {
            _facingDirection = -1.0f;
            _animatedSprite.FlipH = true;
        }

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

    private void StartAttack()
    {
        if (_currentState == PlayerState.Attack)
            return;

        _currentState = PlayerState.Attack;
        Velocity = Vector2.Zero;
        _hitTargets.Clear();
        DisableAttackArea();
        UpdateAttackAreaPosition();
        _animatedSprite.Scale = AttackVisualScale;
        _animatedSprite.Position = AttackVisualOffset;
        _animatedSprite.Play(AttackAnimation);
    }

    private void OnAnimatedSpriteFrameChanged()
    {
        if (
            _currentState == PlayerState.Attack
            && _animatedSprite.Animation == AttackAnimation
            && _animatedSprite.Frame == AttackActiveFrame
        )
        {
            EnableAttackArea();
        }
        else
        {
            DisableAttackArea();
        }
    }

    private void EnableAttackArea()
    {
        UpdateAttackAreaPosition();
        _attackShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _attackArea.SetDeferred(Area2D.PropertyName.Monitoring, true);
    }

    private void DisableAttackArea()
    {
        _attackShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _attackArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
    }

    private void UpdateAttackAreaPosition()
    {
        Vector2 position = _attackArea.Position;
        position.X = AttackOffsetX * _facingDirection;
        _attackArea.Position = position;
    }

    private void OnAttackAreaAreaEntered(Area2D area)
    {
        if (_currentState != PlayerState.Attack || _animatedSprite.Frame != AttackActiveFrame)
            return;

        Node target = area.GetParent();
        if (_hitTargets.Contains(target) || target is not IDamageable damageable)
            return;

        _hitTargets.Add(target);
        damageable.TakeDamage(AttackDamage);
    }

    private void OnAnimationFinished()
    {
        if (_currentState != PlayerState.Attack || _animatedSprite.Animation != AttackAnimation)
            return;

        DisableAttackArea();
        _hitTargets.Clear();
        _currentState = PlayerState.Idle;
        _animatedSprite.Scale = NormalVisualScale;
        _animatedSprite.Position = NormalVisualOffset;
        _animatedSprite.Play(IdleAnimation);
        ClampToViewport();
    }

    private void ClampToViewport()
    {
        Rect2 viewportRect = GetViewportRect();
        Texture2D currentTexture = _animatedSprite.SpriteFrames.GetFrameTexture(
            _animatedSprite.Animation,
            _animatedSprite.Frame
        );
        Vector2 halfSpriteSize = currentTexture.GetSize() * _animatedSprite.Scale.Abs() / 2.0f;
        Vector2 visualOffset = _animatedSprite.Position;
        Vector2 minimumPosition = viewportRect.Position + halfSpriteSize - visualOffset;
        Vector2 maximumPosition = viewportRect.End - halfSpriteSize - visualOffset;

        GlobalPosition = GlobalPosition.Clamp(minimumPosition, maximumPosition);
    }
}
