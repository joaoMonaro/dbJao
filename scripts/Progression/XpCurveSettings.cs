using System;

public sealed class XpCurveSettings
{
    public static XpCurveSettings Default { get; } = new(100, 1.01, 200);

    public long BaseXp { get; }
    public double GrowthRate { get; }
    public int LevelsPerReset { get; }

    public XpCurveSettings(long baseXp, double growthRate, int levelsPerReset)
    {
        if (baseXp <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseXp));
        if (!double.IsFinite(growthRate) || growthRate <= 1.0)
            throw new ArgumentOutOfRangeException(nameof(growthRate));
        if (levelsPerReset <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelsPerReset));

        BaseXp = baseXp;
        GrowthRate = growthRate;
        LevelsPerReset = levelsPerReset;
    }
}
