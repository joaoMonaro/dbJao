using System;

public sealed class BattlePowerProgression(BattlePowerSettings settings)
{
    public BattlePowerSettings Settings { get; } = settings;

    public long FromCompletedLevels(long completedLevels)
    {
        if (completedLevels < 0)
            throw new ArgumentOutOfRangeException(nameof(completedLevels));

        long gained = checked(completedLevels * Settings.BattlePowerPerLevel);
        return checked(Settings.InitialBattlePower + gained);
    }

    public long AddCompletedLevels(long currentBattlePower, int completedLevels)
    {
        if (currentBattlePower < Settings.InitialBattlePower)
            throw new ArgumentOutOfRangeException(nameof(currentBattlePower));
        if (completedLevels < 0)
            throw new ArgumentOutOfRangeException(nameof(completedLevels));

        long gained = checked((long)completedLevels * Settings.BattlePowerPerLevel);
        return checked(currentBattlePower + gained);
    }
}
