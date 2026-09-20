using System;

public readonly record struct EnemyCombatStats(long Attack, long Defense, int MaxHealth);

public sealed class EnemyDefinition
{
    public string Id { get; }
    public string Name { get; }
    public long CombatPower { get; }
    public decimal AttackMultiplier { get; }
    public decimal DefenseMultiplier { get; }
    public decimal HealthMultiplier { get; }
    public long XpReward { get; }
    public float MoveSpeed { get; }
    public float DetectionRange { get; }
    public float AttackRange { get; }
    public float AttackCooldown { get; }

    public EnemyDefinition(
        string id,
        string name,
        long combatPower,
        decimal attackMultiplier,
        decimal defenseMultiplier,
        decimal healthMultiplier,
        long xpReward,
        float moveSpeed,
        float detectionRange,
        float attackRange,
        float attackCooldown)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id obrigatório.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome obrigatório.", nameof(name));
        if (combatPower <= 0)
            throw new ArgumentOutOfRangeException(nameof(combatPower));
        if (attackMultiplier <= 0 || defenseMultiplier < 0 || healthMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(attackMultiplier));
        if (xpReward < 0)
            throw new ArgumentOutOfRangeException(nameof(xpReward));
        if (!float.IsFinite(moveSpeed) || moveSpeed <= 0
            || !float.IsFinite(detectionRange) || detectionRange <= 0
            || !float.IsFinite(attackRange) || attackRange <= 0
            || !float.IsFinite(attackCooldown) || attackCooldown <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moveSpeed));
        }

        Id = id.Trim();
        Name = name.Trim();
        CombatPower = combatPower;
        AttackMultiplier = attackMultiplier;
        DefenseMultiplier = defenseMultiplier;
        HealthMultiplier = healthMultiplier;
        XpReward = xpReward;
        MoveSpeed = moveSpeed;
        DetectionRange = detectionRange;
        AttackRange = attackRange;
        AttackCooldown = attackCooldown;
    }

    public EnemyCombatStats CalculateCombatStats() => new(
        ScaleToLong(CombatPower, AttackMultiplier),
        ScaleToLong(CombatPower, DefenseMultiplier),
        ScaleToHealth(CombatPower, HealthMultiplier));

    private static long ScaleToLong(long value, decimal multiplier)
    {
        decimal result = decimal.Floor(checked(value * multiplier));
        if (result > long.MaxValue)
            throw new OverflowException("Atributo do inimigo excedeu Int64.");
        return decimal.ToInt64(result);
    }

    private static int ScaleToHealth(long value, decimal multiplier)
    {
        long result = ScaleToLong(value, multiplier);
        if (result > int.MaxValue)
            throw new OverflowException("Vida do inimigo excedeu Int32.");
        return checked((int)result);
    }
}
