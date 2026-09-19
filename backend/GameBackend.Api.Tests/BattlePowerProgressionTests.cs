using Xunit;
using GameBackend.Api.Entities;

namespace GameBackend.Api.Tests;

public sealed class BattlePowerProgressionTests
{
    private static readonly BattlePowerProgression Progression =
        new(BattlePowerSettings.Default);

    [Fact]
    public void DefaultConfigurationStartsAtTenAndGrantsOneHundredPerLevel()
    {
        Assert.Equal(10, BattlePowerSettings.Default.InitialBattlePower);
        Assert.Equal(100, BattlePowerSettings.Default.BattlePowerPerLevel);
        Assert.Equal(10, Progression.FromCompletedLevels(0));
        Assert.Equal(110, Progression.AddCompletedLevels(10, 1));
    }

    [Fact]
    public void NewPersistentCharacterStartsWithConfiguredInitialValue()
    {
        Character character = new();

        Assert.Equal(BattlePowerSettings.Default.InitialBattlePower,
            character.BaseBattlePower);
        Assert.Equal(CharacterRegistry.DefaultCharacterId, character.ActiveCharacterId);
    }

    [Theory]
    [InlineData(5, 510)]
    [InlineData(200, 20010)]
    [InlineData(450, 45010)]
    public void CompletedLevelsGrantExactlyOneIncrementEach(long levels, long expected)
    {
        Assert.Equal(expected, Progression.FromCompletedLevels(levels));
    }

    [Fact]
    public void NoCompletedLevelDoesNotChangeBattlePower()
    {
        Assert.Equal(12310, Progression.AddCompletedLevels(12310, 0));
    }

    [Fact]
    public void ResetHasNoSeparateBonus()
    {
        long throughTwoResetsAndTwentyLevels = Progression.FromCompletedLevels(420);

        Assert.Equal(42010, throughTwoResetsAndTwentyLevels);
    }

    [Fact]
    public void InvalidOrOverflowingValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Progression.FromCompletedLevels(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Progression.AddCompletedLevels(9, 1));
        Assert.Throws<OverflowException>(() => Progression.FromCompletedLevels(long.MaxValue));
        Assert.Throws<OverflowException>(() =>
            Progression.AddCompletedLevels(long.MaxValue, 1));
    }
}
