using System;

public sealed class BattlePowerSettings
{
    public static BattlePowerSettings Default { get; } = new(10, 100);

    public long InitialBattlePower { get; }
    public long BattlePowerPerLevel { get; }

    public BattlePowerSettings(long initialBattlePower, long battlePowerPerLevel)
    {
        if (initialBattlePower < 0)
            throw new ArgumentOutOfRangeException(nameof(initialBattlePower));
        if (battlePowerPerLevel <= 0)
            throw new ArgumentOutOfRangeException(nameof(battlePowerPerLevel));

        InitialBattlePower = initialBattlePower;
        BattlePowerPerLevel = battlePowerPerLevel;
    }
}
