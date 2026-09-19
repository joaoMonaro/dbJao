using System;
using System.Collections.Generic;

public readonly record struct ProgressionState(long TotalXp, int Level, long Reset);
public readonly record struct ProgressionSnapshot(
    ProgressionState State,
    long XpIntoLevel,
    long XpRequiredForNextLevel);
public readonly record struct ProgressionStep(int Level, long Reset, bool ResetCompleted);
public sealed record ProgressionResult(
    ProgressionSnapshot Snapshot,
    IReadOnlyList<ProgressionStep> Steps);

public sealed class CharacterProgression(ExponentialXpCurve curve)
{
    public ProgressionSnapshot FromTotalXp(long totalXp)
    {
        if (totalXp < 0)
            throw new ArgumentOutOfRangeException(nameof(totalXp));

        long remaining = totalXp;
        int level = 0;
        long reset = 0;

        while (true)
        {
            long required = curve.GetRequiredXpForNextLevel(reset, level);
            if (remaining < required)
                return new(new(totalXp, level, reset), remaining, required);

            remaining -= required;
            Advance(ref level, ref reset);
        }
    }

    public ProgressionResult AddXp(ProgressionState current, long amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (current.Level < 0 || current.Level >= curve.Settings.LevelsPerReset)
            throw new ArgumentOutOfRangeException(nameof(current));
        if (current.Reset < 0)
            throw new ArgumentOutOfRangeException(nameof(current));

        ProgressionSnapshot before = FromTotalXp(current.TotalXp);
        if (before.State != current)
            throw new ArgumentException("Estado de progressão incompatível com TotalXp.", nameof(current));
        if (amount == 0)
            return new(before, Array.Empty<ProgressionStep>());

        long totalXp = checked(current.TotalXp + amount);
        long remaining = checked(before.XpIntoLevel + amount);
        int level = current.Level;
        long reset = current.Reset;
        List<ProgressionStep> steps = [];

        while (true)
        {
            long required = curve.GetRequiredXpForNextLevel(reset, level);
            if (remaining < required)
            {
                ProgressionSnapshot snapshot = new(
                    new(totalXp, level, reset), remaining, required);
                return new(snapshot, steps);
            }

            remaining -= required;
            bool completedReset = level == curve.Settings.LevelsPerReset - 1;
            Advance(ref level, ref reset);
            steps.Add(new(level, reset, completedReset));
        }
    }

    private void Advance(ref int level, ref long reset)
    {
        if (level == curve.Settings.LevelsPerReset - 1)
        {
            reset = checked(reset + 1);
            level = 0;
        }
        else
        {
            level++;
        }
    }
}
