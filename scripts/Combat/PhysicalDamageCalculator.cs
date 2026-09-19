using System;

public interface IPhysicalDamageCalculator
{
    long Calculate(long attack, long defense, decimal attackMultiplier);
}

public sealed class PhysicalDamageCalculator : IPhysicalDamageCalculator
{
    public long Calculate(long attack, long defense, decimal attackMultiplier)
    {
        if (attack <= 0)
            throw new ArgumentOutOfRangeException(nameof(attack), "Attack deve ser positivo.");
        if (defense < 0)
            throw new ArgumentOutOfRangeException(nameof(defense), "Defense não pode ser negativo.");
        if (attackMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attackMultiplier),
                "O multiplicador do ataque deve ser positivo.");
        }

        decimal attackValue = attack;
        decimal denominator = attackValue + defense;

        decimal calculatedDamage;
        try
        {
            calculatedDamage = checked(
                attackValue * (attackValue / denominator) * attackMultiplier);
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }

        if (calculatedDamage < 1m)
            return 1;

        decimal flooredDamage = decimal.Floor(calculatedDamage);
        return flooredDamage >= long.MaxValue
            ? long.MaxValue
            : decimal.ToInt64(flooredDamage);
    }
}
