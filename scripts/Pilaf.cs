using Godot;

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

	private Area2D? _damageArea;
	private Timer? _damageTimer;
	private Node2D? _target;
	private Node? _contactTarget;

	public override void _Ready()
	{
		base._Ready();

		_damageArea = GetNodeOrNull<Area2D>("DamageArea");
		_damageTimer = GetNodeOrNull<Timer>("DamageTimer");

		if (AnimatedSprite is not null)
			AnimatedSprite.AnimationFinished += OnAnimationFinished;

		if (!CanRunServerAi())
		{
			_damageTimer?.Stop();
			return;
		}

		if (_damageArea is null || _damageTimer is null)
		{
			GD.PushWarning($"[SERVER] Nodes de combate do Pilaf ausentes: {GetPath()}");
		}
		else
		{
			_damageArea.BodyEntered += OnDamageAreaBodyEntered;
			_damageArea.BodyExited += OnDamageAreaBodyExited;
			_damageTimer.Timeout += OnDamageTimerTimeout;
			_damageTimer.WaitTime = Mathf.Max(ContactDamageInterval, 0.01f);
		}

		FindTarget();
		LogServerAiActive();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CanRunServerAi())
		{
			UpdateClientPresentation();
			return;
		}

		if (IsDead)
			return;

		if (!GodotObject.IsInstanceValid(_target))
			FindTarget();

		if (!GodotObject.IsInstanceValid(_target))
		{
			Velocity = Vector2.Zero;
			UpdateServerMovementState(Vector2.Zero);
			return;
		}

		MovementDirection = GlobalPosition.DirectionTo(_target!.GlobalPosition).LimitLength(1.0f);
		Velocity = MovementDirection * MoveSpeed;
		MoveAndSlide();
		KeepInsideViewport();
		UpdateServerMovementState(MovementDirection);
	}

	private void FindTarget()
	{
		if (!CanRunServerAi())
			return;

		_target = GetTree().GetFirstNodeInGroup(TargetGroup) as Node2D;
	}

	private void OnDamageAreaBodyEntered(Node2D body)
	{
		if (!CanRunServerAi() || IsDead || !body.IsInGroup(TargetGroup))
			return;

		_contactTarget = body;
		DealContactDamage();
		_damageTimer?.Start(Mathf.Max(ContactDamageInterval, 0.01f));
	}

	private void OnDamageAreaBodyExited(Node2D body)
	{
		if (!CanRunServerAi() || body != _contactTarget)
			return;

		_contactTarget = null;
		_damageTimer?.Stop();
	}

	private void OnDamageTimerTimeout()
	{
		if (!CanRunServerAi() || IsDead || !GodotObject.IsInstanceValid(_contactTarget))
			return;

		DealContactDamage();
	}

	private void DealContactDamage()
	{
		if (!CanRunServerAi() || Multiplayer.HasMultiplayerPeer())
			return;

		if (ContactDamage <= 0 || !GodotObject.IsInstanceValid(_contactTarget))
			return;

		if (_contactTarget is not IDamageable damageable)
			return;

		damageable.TakeDamage(ContactDamage);

		if (AnimatedSprite is not null)
		{
			AnimatedSprite.Scale = AttackVisualScale;
			AnimatedSprite.Position = AttackVisualOffset;
			AnimatedSprite.Play(AttackAnimation);
		}
	}

	private void OnAnimationFinished()
	{
		if (AnimatedSprite is null || AnimatedSprite.Animation != AttackAnimation)
			return;

		AnimatedSprite.Scale = NormalVisualScale;
		AnimatedSprite.Position = NormalVisualOffset;
		AnimatedSprite.Play(IdleAnimation);
	}

	protected override void OnRespawned()
	{
		if (!CanRunServerAi() || AnimatedSprite is null)
			return;

		AnimatedSprite.Scale = NormalVisualScale;
		AnimatedSprite.Position = NormalVisualOffset;
		AnimatedSprite.Play(IdleAnimation);
	}
}
