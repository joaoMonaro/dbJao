using GameBackend.Api.DTOs.GameSessions;
using GameBackend.Api.Entities;
using GameBackend.Api.Repositories;
using GameBackend.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameBackend.Api.Tests;

public sealed class CharacterStateServiceTests
{
    [Fact]
    public async Task SaveForwardsProgressionToPersistence()
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        Guid userId = Guid.NewGuid();
        Guid characterId = Guid.NewGuid();
        SaveCharacterStateRequest request = new(
            userId, 75, 12, 3, 1234567, 3210, "goku",
            ["bear_thief", "bear_thief"],
            "clean_path", 3000, 600);

        Assert.True(await service.SaveAsync(characterId, request, CancellationToken.None));
        Assert.NotNull(repository.Saved);
        Assert.Equal((characterId, userId, 75, 12, 3L, 1234567L, 3210L, "goku",
                "clean_path", 3000f, 600f), repository.Saved.Value.State);
        Assert.Equal(["bear_thief"], repository.Saved.Value.CompletedStages);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(200, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public async Task InvalidProgressionIsRejected(int level, long reset, long totalXp)
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        SaveCharacterStateRequest request = new(
            Guid.NewGuid(), 100, level, reset, totalXp, 10, "goku", [],
            "kame_house", 10, 20);

        Assert.False(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task BattlePowerBelowInitialValueIsRejected()
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        SaveCharacterStateRequest request = new(
            Guid.NewGuid(), 100, 0, 0, 0, 9, "goku", [], "kame_house", 10, 20);

        Assert.False(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task EmptyActiveCharacterIsRejected()
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        SaveCharacterStateRequest request = new(
            Guid.NewGuid(), 100, 0, 0, 0, 10, "  ", [], "kame_house", 10, 20);

        Assert.False(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Null(repository.Saved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bear-thief")]
    [InlineData("BEAR_THIEF")]
    public async Task InvalidCompletedStageIsRejected(string stageId)
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        SaveCharacterStateRequest request = new(
            Guid.NewGuid(), 100, 0, 0, 0, 10, "goku", [stageId],
            "kame_house", 10, 20);

        Assert.False(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task MissingCompletedStagesIsAcceptedForRollingCompatibility()
    {
        RecordingRepository repository = new();
        CharacterStateService service = new(repository,
            NullLogger<CharacterStateService>.Instance);
        SaveCharacterStateRequest request = new(
            Guid.NewGuid(), 100, 0, 0, 0, 10, "goku", null,
            "kame_house", 10, 20);

        Assert.True(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Empty(repository.Saved!.Value.CompletedStages);
    }

    private sealed class RecordingRepository : ICharacterRepository
    {
        public ((Guid, Guid, int, int, long, long, long, string, string, float, float) State,
            IReadOnlyCollection<string> CompletedStages)? Saved
        { get; private set; }

        public Task<bool> UpdateStateAsync(Guid characterId, Guid userId, int currentHealth,
            int level, long reset, long totalXp, long baseBattlePower,
            string activeCharacterId, IReadOnlyCollection<string> completedStages,
            string mapId, float positionX, float positionY,
            CancellationToken cancellationToken)
        {
            Saved = ((characterId, userId, currentHealth, level, reset, totalXp,
                baseBattlePower, activeCharacterId, mapId, positionX, positionY),
                completedStages);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Character>> GetAllByUserIdAsync(Guid userId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Character?> GetByIdForUserAsync(Guid id, Guid userId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> CountByUserIdAsync(Guid userId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> NameExistsForUserAsync(Guid userId, string name,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(Character character,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
