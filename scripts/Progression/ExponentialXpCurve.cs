using System;

public sealed class ExponentialXpCurve(XpCurveSettings settings)
{
    public XpCurveSettings Settings { get; } = settings;

    public long GetGlobalLevel(long reset, int level)
    {
        if (reset < 0)
            throw new ArgumentOutOfRangeException(nameof(reset));
        if (level < 0 || level >= Settings.LevelsPerReset)
            throw new ArgumentOutOfRangeException(nameof(level));

        if (reset > (long.MaxValue - level) / Settings.LevelsPerReset)
            return long.MaxValue;

        return reset * Settings.LevelsPerReset + level;
    }

    public long GetRequiredXpForNextLevel(long reset, int level)
    {
        long globalLevel = GetGlobalLevel(reset, level);
        double required = Settings.BaseXp * Math.Pow(Settings.GrowthRate, globalLevel);

        // A partir daqui nenhum TotalXp representável em long alcança o próximo nível.
        if (!double.IsFinite(required) || required >= long.MaxValue)
            return long.MaxValue;

        return Math.Max(1L, (long)Math.Ceiling(required));
    }
}
