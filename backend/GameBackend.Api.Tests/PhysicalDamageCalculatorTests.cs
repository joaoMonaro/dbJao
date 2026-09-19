using Xunit;

namespace GameBackend.Api.Tests;

public sealed class PhysicalDamageCalculatorTests
{
    private static readonly PhysicalDamageCalculator Calculator = new();

    [Theory]
    [InlineData(10_000, 10_000, 5_000)]
    [InlineData(20_000, 10_000, 13_333)]
    [InlineData(10_000, 20_000, 3_333)]
    public void Calculate_UsesAttackAndDefense(long attack, long defense, long expected)
    {
        Assert.Equal(expected, Calculator.Calculate(attack, defense, 1.0m));
    }

    [Fact]
    public void Calculate_AppliesAttackMultiplier()
    {
        Assert.Equal(7_500, Calculator.Calculate(10_000, 10_000, 1.5m));
    }

    [Fact]
    public void Calculate_FloorsFractionalDamage()
    {
        Assert.Equal(5_000, Calculator.Calculate(10_000, 10_000, 1.0001m));
    }

    [Fact]
    public void Calculate_AppliesMinimumDamageToValidAttack()
    {
        Assert.Equal(1, Calculator.Calculate(1, long.MaxValue, 1.0m));
    }

    [Fact]
    public void Calculate_UsesCombatStatsDerivedFromGoku()
    {
        CombatStats stats = new CombatStatsCalculator().Calculate(
            10_000,
            CharacterRegistry.Get("goku"));

        Assert.Equal(5_000, Calculator.Calculate(stats.Attack, stats.Defense, 1.0m));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void Calculate_RejectsNonPositiveAttack(long attack, long defense)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Calculator.Calculate(attack, defense, 1.0m));
    }

    [Fact]
    public void Calculate_RejectsNegativeDefense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Calculator.Calculate(10, -1, 1.0m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_RejectsNonPositiveMultiplier(int multiplier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Calculator.Calculate(10, 10, multiplier));
    }

    [Fact]
    public void Calculate_SaturatesWhenMultiplierExceedsLongRange()
    {
        Assert.Equal(
            long.MaxValue,
            Calculator.Calculate(long.MaxValue, 0, decimal.MaxValue));
    }
}
