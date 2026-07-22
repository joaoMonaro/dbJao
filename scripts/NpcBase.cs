using Godot;

public partial class NpcBase : CharacterBody2D, IDamageable
{
	public const int IdleAiState = 0;
	public const int MovingAiState = 1;

	[Export] public float MoveSpeed { get; set; } = 40.0f;
	[Export] public int MaxHealth { get; set; } = 100;
	[Export] public float RespawnDelay { get; set; } = 5.0f;
	[Export] public Vector2 MovementDirection { get; set; } = Vector2.Zero;
	[Export(PropertyHint.Enum, "Idle,Moving")] public int AiState { get; set; } = IdleAiState;

	public bool NetworkSetupIsValid { get; private set; }

	protected Sprite2D? Sprite;
	protected AnimatedSprite2D? AnimatedSprite;
	protected bool IsDead;

	private CollisionShape2D? _bodyShape;
	private Area2D? _hurtbox;
	private CollisionShape2D? _hurtboxShape;
	private ProgressBar? _healthBar;
	private int _currentHealth;
	private Vector2 _spawnPosition;
	private bool _clientSynchronizationLogged;
	private double _clientSynchronizationDelay;
	private bool _invalidAuthorityLogged;

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(NetworkConstants.ServerPeerId);
	}

	public override void _Ready()
	{
		Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		AnimatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_bodyShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		_hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
		_hurtboxShape = GetNodeOrNull<CollisionShape2D>("Hurtbox/CollisionShape2D");
		_healthBar = GetNodeOrNull<ProgressBar>("HealthBar");

		_spawnPosition = GlobalPosition;
		_currentHealth = Mathf.Max(MaxHealth, 0);
		UpdateHealthBar();
		NetworkSetupIsValid = ValidateNetworkSetup();

		if (NetworkManager.RunningAsServer)
			GD.Print($"[SERVER] NPC inicializado: {Name}");
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
				+ $"| direção: {MovementDirection} | estado: {AiState}"
			);
		}
	}

	protected void LogServerAiActive()
	{
		GD.Print($"[SERVER] IA ativa para: {Name}");
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
		if (_healthBar is null)
			return;

		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = _currentHealth;
	}

	private async void Die()
	{
		if (IsDead)
			return;

		IsDead = true;
		MovementDirection = Vector2.Zero;
		AiState = IdleAiState;
		Velocity = Vector2.Zero;
		SetPhysicsProcess(false);
		_bodyShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hurtboxShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_hurtbox?.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_hurtbox?.SetDeferred(Area2D.PropertyName.Monitorable, false);
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
		_bodyShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_hurtboxShape?.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_hurtbox?.SetDeferred(Area2D.PropertyName.Monitoring, true);
		_hurtbox?.SetDeferred(Area2D.PropertyName.Monitorable, true);
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

	private bool ValidateNetworkSetup()
	{
		bool isValid = true;

		if (_bodyShape is null)
		{
			GD.PushError($"[NPC] CollisionShape2D não encontrado: {GetPath()}");
			isValid = false;
		}

		if (Sprite is null && AnimatedSprite is null)
			GD.PushWarning($"[NPC] Node visual não encontrado; headless continuará ativo: {GetPath()}");

		MultiplayerSynchronizer? synchronizer =
			GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		if (synchronizer is null)
		{
			GD.PushError($"[NPC] MultiplayerSynchronizer não encontrado: {GetPath()}");
			return false;
		}

		if (synchronizer.RootPath != new NodePath(".."))
		{
			GD.PushError($"[NPC] RootPath inválido no MultiplayerSynchronizer: {GetPath()}");
			isValid = false;
		}

		SceneReplicationConfig? replicationConfig = synchronizer.ReplicationConfig;
		if (replicationConfig is null)
		{
			GD.PushError($"[NPC] SceneReplicationConfig ausente: {GetPath()}");
			return false;
		}

		NodePath[] expectedProperties =
		{
			new(".:position"),
			new(".:velocity"),
			new(".:MovementDirection"),
			new(".:AiState"),
		};

		Godot.Collections.Array<NodePath> configuredProperties = replicationConfig.GetProperties();
		foreach (NodePath expectedProperty in expectedProperties)
		{
			if (configuredProperties.Contains(expectedProperty))
				continue;

			GD.PushError(
				$"[NPC] Propriedade de replicação ausente ({expectedProperty}): {GetPath()}"
			);
			isValid = false;
		}

		return isValid;
	}
}
