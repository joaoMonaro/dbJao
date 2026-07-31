using System.Threading;
using System.Threading.Tasks;

public sealed class CharacterPersistenceService(IGameBackendClient backendClient)
    : ICharacterPersistenceService
{
    public Task<bool> SaveCharacterStateAsync(
        AuthenticatedPlayerState state,
        CancellationToken cancellationToken = default) =>
        backendClient.SaveCharacterStateAsync(state, cancellationToken);
}
