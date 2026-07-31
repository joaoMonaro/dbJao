using System.Threading;
using System.Threading.Tasks;

public interface ICharacterPersistenceService
{
    Task<bool> SaveCharacterStateAsync(
        AuthenticatedPlayerState state,
        CancellationToken cancellationToken = default);
}
