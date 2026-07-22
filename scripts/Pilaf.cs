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

	private Area2D _damageArea = null!;
	private Timer _damageTimer = null!;
	private Node2D? _target;
	private Node? _contactTarget;

	public override void _Ready()
	{
		base._Ready();

		_damageArea = GetNode<Area2D>("DamageArea");
		_damageTimer = GetNode<Timer>("DamageTimer");
		_damageArea.BodyEntered += OnDamageAreaBodyEntered;
		_damageArea.BodyExited += OnDamageAreaBodyExited;
		_damageTimer.Timeout += OnDamageTimerTimeout;
		_damageTimer.WaitTime = Mathf.Max(ContactDamageInterval, 0.01f);

		if (AnimatedSprite is not null)
			AnimatedSprite.AnimationFinished += OnAnimationFinished;

		FindTarget();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
			return;

		if (!GodotObject.IsInstanceValid(_target))
			FindTarget();

		if (!GodotObject.IsInstanceValid(_target))
		{
			Velocity = Vector2.Zero;
			return;
		}

		CurrentDirection = GlobalPosition.DirectionTo(_target!.GlobalPosition);
		Velocity = CurrentDirection * MoveSpeed;
		MoveAndSlide();
		KeepInsideViewport();
		UpdateSpriteDirection();
	}

	private void FindTarget()
	{
		_target = GetTree().GetFirstNodeInGroup(TargetGroup) as Node2D;
	}

	private void OnDamageAreaBodyEntered(Node2D body)
	{
		if (IsDead || !body.IsInGroup(TargetGroup))
			return;

		_contactTarget = body;
		DealContactDamage();
		_damageTimer.Start(Mathf.Max(ContactDamageInterval, 0.01f));
	}

	private void OnDamageAreaBodyExited(Node2D body)
	{
		if (body != _contactTarget)
			return;

		_contactTarget = null;
		_damageTimer.Stop();
	}

	private void OnDamageTimerTimeout()
	{
		if (IsDead || !GodotObject.IsInstanceValid(_contactTarget))
			return;

		DealContactDamage();
	}

	private void DealContactDamage()
	{
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
		if (AnimatedSprite is null)
			return;

		AnimatedSprite.Scale = NormalVisualScale;
		AnimatedSprite.Position = NormalVisualOffset;
		AnimatedSprite.Play(IdleAnimation);
	}
}
