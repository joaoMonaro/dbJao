using System;

public sealed class CombatStatsCalculator
{
    public CombatStats Calculate(long effectiveBattlePower, CharacterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (effectiveBattlePower < 0)
            throw new ArgumentOutOfRangeException(nameof(effectiveBattlePower));

        return new(
            ScaleAndFloor(effectiveBattlePower, definition.AttackMultiplier),
            ScaleAndFloor(effectiveBattlePower, definition.DefenseMultiplier),
            ScaleAndFloor(effectiveBattlePower, definition.KiAttackMultiplier));
    }

    private static long ScaleAndFloor(long battlePower, decimal multiplier)
    {
        decimal scaled;
        try
        {
            scaled = decimal.Floor(checked(battlePower * multiplier));
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }

        return scaled >= long.MaxValue ? long.MaxValue : decimal.ToInt64(scaled);
    }
}
