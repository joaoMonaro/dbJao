using Xunit;

namespace GameBackend.Api.Tests;

public sealed class CharacterProgressionTests
{
    private static readonly ExponentialXpCurve Curve = new(XpCurveSettings.Default);
    private static readonly CharacterProgression Progression = new(Curve);

    [Fact]
    public void InsufficientAndExactXpKeepHistoricalTotal()
    {
        ProgressionState initial = new(0, 0, 0);
        ProgressionResult partial = Progression.AddXp(initial, 99);
        Assert.Equal(new ProgressionState(99, 0, 0), partial.Snapshot.State);
        Assert.Equal(99, partial.Snapshot.XpIntoLevel);
        Assert.Empty(partial.Steps);

        ProgressionResult completed = Progression.AddXp(partial.Snapshot.State, 1);
        Assert.Equal(new ProgressionState(100, 1, 0), completed.Snapshot.State);
        Assert.Equal(0, completed.Snapshot.XpIntoLevel);
        Assert.Single(completed.Steps);
    }

    [Fact]
    public void SurplusXpCrossesSeveralLevelsWithoutLoss()
    {
        ProgressionResult result = Progression.AddXp(new(0, 0, 0), 550);
        Assert.Equal(550, result.Snapshot.State.TotalXp);
        Assert.Equal(5, result.Snapshot.State.Level);
        Assert.Equal(0, result.Snapshot.State.Reset);
        Assert.Equal(37, result.Snapshot.XpIntoLevel);
        Assert.Equal(result.Snapshot, Progression.FromTotalXp(550));
        Assert.Equal(result.Snapshot.State.Level, result.Steps.Count);
    }

    [Fact]
    public void CompletingLevel199StartsNextReset()
    {
        long totalBefore = XpForLevels(199);
        ProgressionSnapshot before = Progression.FromTotalXp(totalBefore);
        Assert.Equal(new ProgressionState(totalBefore, 199, 0), before.State);

        ProgressionResult result = Progression.AddXp(before.State, before.XpRequiredForNextLevel);
        Assert.Equal(new ProgressionState(totalBefore + before.XpRequiredForNextLevel, 0, 1),
            result.Snapshot.State);
        Assert.Single(result.Steps);
        Assert.True(result.Steps[0].ResetCompleted);
    }

    [Fact]
    public void LargeGrantCrossesResetAndContinues()
    {
        ProgressionSnapshot before = Progression.FromTotalXp(XpForLevels(198));
        long goal = XpForLevels(203);
        ProgressionResult result = Progression.AddXp(before.State, goal - before.State.TotalXp);

        Assert.Equal(new ProgressionState(goal, 3, 1), result.Snapshot.State);
        Assert.Equal(5, result.Steps.Count);
        Assert.Single(result.Steps, step => step.ResetCompleted);
        Assert.Equal(0, result.Snapshot.XpIntoLevel);
    }

    [Fact]
    public void OneGrantCanCrossMultipleResets()
    {
        long total = XpForLevels(405) + 7;
        ProgressionResult result = Progression.AddXp(new(0, 0, 0), total);

        Assert.Equal(new ProgressionState(total, 5, 2), result.Snapshot.State);
        Assert.Equal(7, result.Snapshot.XpIntoLevel);
        Assert.Equal(405, result.Steps.Count);
        Assert.Equal(2, result.Steps.Count(step => step.ResetCompleted));
        Assert.All(result.Steps, step => Assert.InRange(step.Level, 0, 199));
    }

    [Fact]
    public void GlobalLevelIncludesResetsAndCurveGetsHarder()
    {
        Assert.Equal(0, Curve.GetGlobalLevel(0, 0));
        Assert.Equal(199, Curve.GetGlobalLevel(0, 199));
        Assert.Equal(200, Curve.GetGlobalLevel(1, 0));
        Assert.Equal(650, Curve.GetGlobalLevel(3, 50));
        Assert.True(Curve.GetRequiredXpForNextLevel(1, 0)
            > Curve.GetRequiredXpForNextLevel(0, 0));
    }

    [Fact]
    public void InvalidAndZeroGrantsPreserveInvariants()
    {
        ProgressionState initial = new(0, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => Progression.AddXp(initial, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Progression.AddXp(new(0, 200, 0), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Progression.AddXp(new(0, 0, -1), 1));
        Assert.Throws<OverflowException>(() =>
            Progression.AddXp(Progression.FromTotalXp(long.MaxValue).State, 1));

        ProgressionResult unchanged = Progression.AddXp(initial, 0);
        Assert.Equal(initial, unchanged.Snapshot.State);
        Assert.Empty(unchanged.Steps);
    }

    [Fact]
    public void CurveSaturatesSafelyAtLongLimit()
    {
        Assert.Equal(long.MaxValue, Curve.GetRequiredXpForNextLevel(long.MaxValue, 0));
        ProgressionSnapshot snapshot = Progression.FromTotalXp(long.MaxValue);
        Assert.InRange(snapshot.State.Level, 0, 199);
        Assert.True(snapshot.State.Reset >= 0);
        Assert.Equal(long.MaxValue, snapshot.State.TotalXp);
    }

    [Fact]
    public void CurveSettingsCanBeChangedCentrally()
    {
        ExponentialXpCurve custom = new(new XpCurveSettings(50, 1.1, 10));
        Assert.Equal(10, custom.GetGlobalLevel(1, 0));
        Assert.Equal(50, custom.GetRequiredXpForNextLevel(0, 0));
        Assert.True(custom.GetRequiredXpForNextLevel(1, 0) > 50);
    }

    private static long XpForLevels(int count)
    {
        long total = 0;
        for (int globalLevel = 0; globalLevel < count; globalLevel++)
            total = checked(total + Curve.GetRequiredXpForNextLevel(
                globalLevel / Curve.Settings.LevelsPerReset,
                globalLevel % Curve.Settings.LevelsPerReset));
        return total;
    }
}
