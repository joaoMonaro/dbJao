using System;
using System.Collections.Generic;

public sealed class RespawnSlotController
{
    private const double TimerEpsilon = 0.000001;
    public int MaxAlive { get; }
    public double RespawnDelaySeconds { get; }
    public int AliveCount { get; private set; }
    public double RemainingDelaySeconds { get; private set; }

    public RespawnSlotController(int maxAlive, double respawnDelaySeconds)
    {
        if (maxAlive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxAlive));
        if (!double.IsFinite(respawnDelaySeconds) || respawnDelaySeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(respawnDelaySeconds));

        MaxAlive = maxAlive;
        RespawnDelaySeconds = respawnDelaySeconds;
    }

    public int GetInitialSpawnCount(bool hasPlayers) =>
        hasPlayers && AliveCount == 0 ? MaxAlive : 0;

    public bool TryRegisterSpawn()
    {
        if (AliveCount >= MaxAlive)
            return false;

        AliveCount++;
        RemainingDelaySeconds = AliveCount < MaxAlive ? RespawnDelaySeconds : 0;
        return true;
    }

    public void RegisterDeath()
    {
        if (AliveCount <= 0)
            return;

        AliveCount--;
        if (RemainingDelaySeconds <= 0)
            RemainingDelaySeconds = RespawnDelaySeconds;
    }

    public bool Advance(double deltaSeconds, bool hasPlayers)
    {
        if (!hasPlayers || AliveCount >= MaxAlive)
            return false;
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        RemainingDelaySeconds = Math.Max(0, RemainingDelaySeconds - deltaSeconds);
        if (RemainingDelaySeconds <= TimerEpsilon)
            RemainingDelaySeconds = 0;
        return RemainingDelaySeconds <= 0;
    }
}

public sealed class BossRespawnController
{
    private const double TimerEpsilon = 0.000001;
    public double CooldownSeconds { get; }
    public bool IsAlive { get; private set; }
    public double RemainingCooldownSeconds { get; private set; }

    public BossRespawnController(double cooldownSeconds)
    {
        if (!double.IsFinite(cooldownSeconds) || cooldownSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));

        CooldownSeconds = cooldownSeconds;
    }

    public bool CanSpawn(bool hasPlayers) =>
        hasPlayers && !IsAlive && RemainingCooldownSeconds <= TimerEpsilon;

    public bool TryRegisterSpawn(bool hasPlayers)
    {
        if (!CanSpawn(hasPlayers))
            return false;

        IsAlive = true;
        return true;
    }

    public void RegisterDeath()
    {
        if (!IsAlive)
            return;

        IsAlive = false;
        RemainingCooldownSeconds = CooldownSeconds;
    }

    public void Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        RemainingCooldownSeconds = Math.Max(0, RemainingCooldownSeconds - deltaSeconds);
        if (RemainingCooldownSeconds <= TimerEpsilon)
            RemainingCooldownSeconds = 0;
    }
}

public sealed class BossParticipationTracker
{
    private const int FirstClientPeerId = 2;
    private readonly HashSet<int> _participants = [];

    public int Count => _participants.Count;

    public bool Register(int peerId) =>
        peerId >= FirstClientPeerId && _participants.Add(peerId);

    public IReadOnlyList<int> GetEligibleParticipants(Func<int, bool> isEligible)
    {
        ArgumentNullException.ThrowIfNull(isEligible);
        List<int> eligible = [];
        foreach (int peerId in _participants)
        {
            if (isEligible(peerId))
                eligible.Add(peerId);
        }

        eligible.Sort();
        return eligible;
    }
}
