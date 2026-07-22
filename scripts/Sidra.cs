using Godot;

public partial class Sidra : NpcBase
{
    [Export] public float MinDirectionTime { get; set; } = 1.5f;
    [Export] public float MaxDirectionTime { get; set; } = 4.0f;

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
        _random.Randomize();
        ChooseNewDirection();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
            return;

        _directionTimer -= (float)delta;
        if (_directionTimer <= 0.0f)
            ChooseNewDirection();

        Velocity = CurrentDirection * MoveSpeed;
        MoveAndSlide();

        Vector2 avoidanceDirection = GetCollisionAvoidanceDirection();
        avoidanceDirection += KeepInsideViewport();

        if (avoidanceDirection != Vector2.Zero)
            ChooseNewDirection(avoidanceDirection.Normalized());
        else if (GetSlideCollisionCount() > 0)
            ChooseNewDirection();

        UpdateSpriteDirection();
    }

    protected override void OnRespawned()
    {
        ChooseNewDirection();
        UpdateSpriteDirection();
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

        CurrentDirection = candidate != Vector2.Zero ? candidate.Normalized() : Vector2.Zero;

        float minimumTime = Mathf.Min(MinDirectionTime, MaxDirectionTime);
        float maximumTime = Mathf.Max(MinDirectionTime, MaxDirectionTime);
        _directionTimer = _random.RandfRange(
            Mathf.Max(0.1f, minimumTime),
            Mathf.Max(0.1f, maximumTime)
        );
    }
}
