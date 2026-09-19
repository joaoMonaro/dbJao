using System.Collections.Generic;
using Xunit;

namespace GameBackend.Api.Tests;

public sealed class PlayableCharacterTests
{
    private static readonly CombatStatsCalculator Calculator = new();

    [Fact]
    public void RegistryResolvesConfiguredGoku()
    {
        CharacterDefinition goku = CharacterRegistry.Get("goku");

        Assert.Equal("goku", goku.Id);
        Assert.Equal("Goku", goku.Name);
        Assert.Equal(1.00m, goku.AttackMultiplier);
        Assert.Equal(1.00m, goku.DefenseMultiplier);
        Assert.Equal(1.00m, goku.KiAttackMultiplier);
        Assert.Equal(1.00m, goku.MaxHealthMultiplier);
        Assert.Equal("res://scenes/Player.tscn", goku.PlayerScenePath);
        Assert.Equal(
            "res://assets/player_hub/goku_portrait.tres",
            goku.PortraitTexturePath);
    }

    [Fact]
    public void RegistryTreatsUnknownIdExplicitly()
    {
        Assert.False(CharacterRegistry.Contains("vegeta"));
        Assert.False(CharacterRegistry.TryGet("vegeta", out _));
        Assert.Throws<KeyNotFoundException>(() => CharacterRegistry.Get("vegeta"));

        CharacterDefinition fallback = CharacterRegistry.ResolveOrDefault(
            "vegeta",
            out bool usedFallback);
        Assert.True(usedFallback);
        Assert.Same(CharacterRegistry.Default, fallback);
    }

    [Fact]
    public void GokuUsesWholeBattlePowerForEveryStat()
    {
        CombatStats stats = Calculator.Calculate(10_000, CharacterRegistry.Get("goku"));

        Assert.Equal(new CombatStats(10_000, 10_000, 10_000), stats);
    }

    [Fact]
    public void DifferentMultipliersProduceIndependentStats()
    {
        CharacterDefinition definition = TestDefinition(1.40m, 0.70m, 0.10m);

        CombatStats stats = Calculator.Calculate(10_000, definition);

        Assert.Equal(new CombatStats(14_000, 7_000, 1_000), stats);
    }

    [Fact]
    public void DecimalResultsAlwaysRoundDown()
    {
        CharacterDefinition definition = TestDefinition(0.70m, 0.70m, 0.70m);

        CombatStats stats = Calculator.Calculate(12_345, definition);

        Assert.Equal(new CombatStats(8_641, 8_641, 8_641), stats);
    }

    [Fact]
    public void SelectingAnotherDefinitionDoesNotChangeSharedBattlePower()
    {
        const long battlePower = 50_000;
        CharacterDefinition stronger = TestDefinition(1.40m, 0.80m, 1.60m);

        CombatStats gokuStats = Calculator.Calculate(battlePower, CharacterRegistry.Default);
        CombatStats strongerStats = Calculator.Calculate(battlePower, stronger);

        Assert.Equal(50_000, battlePower);
        Assert.Equal(50_000, gokuStats.Attack);
        Assert.Equal(70_000, strongerStats.Attack);
    }

    [Fact]
    public void DerivedStatsSaturateAtLongLimit()
    {
        CharacterDefinition definition = TestDefinition(1.80m, 1.80m, 1.80m);

        CombatStats stats = Calculator.Calculate(long.MaxValue, definition);

        Assert.Equal(new CombatStats(long.MaxValue, long.MaxValue, long.MaxValue), stats);
    }

    private static CharacterDefinition TestDefinition(
        decimal attack,
        decimal defense,
        decimal kiAttack) =>
        new(
            "test",
            "Test",
            attack,
            defense,
            kiAttack,
            1.00m,
            "res://test.tscn",
            "res://test.png");
}
