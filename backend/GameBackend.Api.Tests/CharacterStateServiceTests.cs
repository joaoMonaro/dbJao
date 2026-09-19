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
            userId, 75, 12, 3, 1234567, "clean_path", 3000, 600);

        Assert.True(await service.SaveAsync(characterId, request, CancellationToken.None));
        Assert.Equal((characterId, userId, 75, 12, 3L, 1234567L, "clean_path", 3000f, 600f),
            repository.Saved);
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
            Guid.NewGuid(), 100, level, reset, totalXp, "kame_house", 10, 20);

        Assert.False(await service.SaveAsync(Guid.NewGuid(), request, CancellationToken.None));
        Assert.Null(repository.Saved);
    }

    private sealed class RecordingRepository : ICharacterRepository
    {
        public (Guid, Guid, int, int, long, long, string, float, float)? Saved { get; private set; }

        public Task<bool> UpdateStateAsync(Guid characterId, Guid userId, int currentHealth,
            int level, long reset, long totalXp, string mapId, float positionX,
            float positionY, CancellationToken cancellationToken)
        {
            Saved = (characterId, userId, currentHealth, level, reset, totalXp,
                mapId, positionX, positionY);
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
