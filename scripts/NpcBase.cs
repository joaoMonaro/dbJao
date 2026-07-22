using Godot;

public partial class NpcBase : CharacterBody2D, IDamageable
{
    [Export] public float MoveSpeed { get; set; } = 40.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float RespawnDelay { get; set; } = 5.0f;

    protected Sprite2D? Sprite;
    protected AnimatedSprite2D? AnimatedSprite;
    protected Vector2 CurrentDirection = Vector2.Zero;
    protected bool IsDead;

    private CollisionShape2D _bodyShape = null!;
    private Area2D _hurtbox = null!;
    private CollisionShape2D _hurtboxShape = null!;
    private ProgressBar _healthBar = null!;
    private int _currentHealth;
    private Vector2 _spawnPosition;

    public override void _Ready()
    {
        Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        AnimatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _bodyShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _hurtbox = GetNode<Area2D>("Hurtbox");
        _hurtboxShape = GetNode<CollisionShape2D>("Hurtbox/CollisionShape2D");
        _healthBar = GetNode<ProgressBar>("HealthBar");

        _spawnPosition = GlobalPosition;
        _currentHealth = Mathf.Max(MaxHealth, 0);
        UpdateHealthBar();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        _currentHealth = Mathf.Max(_currentHealth - amount, 0);
        UpdateHealthBar();

        if (_currentHealth <= 0)
            Die();
    }

    private void UpdateHealthBar()
    {
        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = _currentHealth;
    }

    private async void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        CurrentDirection = Vector2.Zero;
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _hurtboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _hurtbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
        _hurtbox.SetDeferred(Area2D.PropertyName.Monitorable, false);
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
        _currentHealth = Mathf.Max(MaxHealth, 0);
        UpdateHealthBar();
        _bodyShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _hurtboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _hurtbox.SetDeferred(Area2D.PropertyName.Monitoring, true);
        _hurtbox.SetDeferred(Area2D.PropertyName.Monitorable, true);
        Visible = true;
        IsDead = false;
        OnRespawned();
        SetPhysicsProcess(true);
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
            Texture2D texture = AnimatedSprite.SpriteFrames.GetFrameTexture(
                AnimatedSprite.Animation,
                AnimatedSprite.Frame
            );
            halfSpriteSize = texture.GetSize() * AnimatedSprite.Scale.Abs() / 2.0f;
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
        if (CurrentDirection.X > 0.0f)
        {
            if (Sprite is not null)
                Sprite.FlipH = false;
            if (AnimatedSprite is not null)
                AnimatedSprite.FlipH = false;
        }
        else if (CurrentDirection.X < 0.0f)
        {
            if (Sprite is not null)
                Sprite.FlipH = true;
            if (AnimatedSprite is not null)
                AnimatedSprite.FlipH = true;
        }
    }
}
