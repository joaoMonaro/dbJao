using Xunit;

namespace GameBackend.Api.Tests;

public sealed class BearThiefStageRulesTests
{
    [Fact]
    public void WolfSlotsNeverExceedFiveAndFillGradually()
    {
        RespawnSlotController rules = new(5, 3);
        Assert.Equal(0, rules.GetInitialSpawnCount(hasPlayers: false));
        Assert.Equal(5, rules.GetInitialSpawnCount(hasPlayers: true));
        for (int index = 0; index < 5; index++)
            Assert.True(rules.TryRegisterSpawn());

        Assert.Equal(5, rules.AliveCount);
        Assert.False(rules.TryRegisterSpawn());
        Assert.False(rules.Advance(30, hasPlayers: true));

        rules.RegisterDeath();
        rules.RegisterDeath();
        rules.RegisterDeath();
        Assert.Equal(2, rules.AliveCount);
        Assert.False(rules.Advance(2.99, hasPlayers: true));
        Assert.True(rules.Advance(0.01, hasPlayers: true));
        Assert.True(rules.TryRegisterSpawn());
        Assert.Equal(3, rules.AliveCount);
        Assert.False(rules.Advance(2.99, hasPlayers: true));
        Assert.True(rules.Advance(0.01, hasPlayers: true));
        Assert.True(rules.TryRegisterSpawn());
        Assert.Equal(4, rules.AliveCount);
    }

    [Fact]
    public void WolfRespawnPausesWithoutPlayers()
    {
        RespawnSlotController rules = new(5, 3);
        for (int index = 0; index < 5; index++)
            rules.TryRegisterSpawn();
        rules.RegisterDeath();

        Assert.False(rules.Advance(30, hasPlayers: false));
        Assert.Equal(3, rules.RemainingDelaySeconds);
        Assert.False(rules.Advance(2.9, hasPlayers: true));
        Assert.True(rules.Advance(0.1, hasPlayers: true));
    }

    [Fact]
    public void BossIsUniqueAndCooldownStartsOnDeath()
    {
        BossRespawnController rules = new(300);
        Assert.True(rules.TryRegisterSpawn(hasPlayers: true));
        Assert.False(rules.TryRegisterSpawn(hasPlayers: true));
        Assert.Equal(0, rules.RemainingCooldownSeconds);

        rules.RegisterDeath();
        Assert.Equal(300, rules.RemainingCooldownSeconds);
        rules.Advance(299.9);
        Assert.False(rules.CanSpawn(hasPlayers: true));
        rules.Advance(0.1);
        Assert.False(rules.CanSpawn(hasPlayers: false));
        Assert.True(rules.CanSpawn(hasPlayers: true));

        BossRespawnController afterRestart = new(300);
        Assert.True(afterRestart.CanSpawn(hasPlayers: true));
    }

    [Fact]
    public void ParticipationIsUniqueAndFilteredAtDeath()
    {
        BossParticipationTracker tracker = new();
        Assert.True(tracker.Register(2));
        Assert.False(tracker.Register(2));
        Assert.True(tracker.Register(3));
        Assert.True(tracker.Register(4));
        Assert.False(tracker.Register(1));

        Assert.Equal([2, 4], tracker.GetEligibleParticipants(peerId => peerId != 3));
    }

    [Theory]
    [InlineData(StageAreaIds.BearThief01, 1, StageAreaIds.BearThief02, StageEntrySide.Left)]
    [InlineData(StageAreaIds.BearThief02, -1, StageAreaIds.BearThief01, StageEntrySide.Right)]
    [InlineData(StageAreaIds.BearThief02, 1, StageAreaIds.BearThief03, StageEntrySide.Left)]
    [InlineData(StageAreaIds.BearThief03, -1, StageAreaIds.BearThief02, StageEntrySide.Right)]
    [InlineData(StageAreaIds.BearThief03, 1, StageAreaIds.BearThiefBoss, StageEntrySide.Left)]
    [InlineData(StageAreaIds.BearThiefBoss, -1, StageAreaIds.BearThief03, StageEntrySide.Right)]
    public void RouteUsesCorrectAreaAndEntrySide(
        string source,
        int direction,
        string target,
        StageEntrySide entrySide)
    {
        Assert.True(StageRoute.TryGetTransition(source, direction, out StageTransition result));
        Assert.Equal(target, result.TargetAreaId);
        Assert.Equal(entrySide, result.EntrySide);
    }

    [Fact]
    public void EnemyDefinitionsProduceConfiguredStats()
    {
        EnemyCombatStats wolf = EnemyRegistry.Get(EnemyRegistry.WolfId).CalculateCombatStats();
        EnemyCombatStats bear = EnemyRegistry.Get(EnemyRegistry.BearThiefId)
            .CalculateCombatStats();

        Assert.Equal(new EnemyCombatStats(1200, 900, 1500), wolf);
        Assert.Equal(new EnemyCombatStats(5000, 4500, 25000), bear);
        Assert.Equal(25, EnemyRegistry.Get(EnemyRegistry.WolfId).XpReward);
        Assert.Equal(500, EnemyRegistry.Get(EnemyRegistry.BearThiefId).XpReward);
    }
}
