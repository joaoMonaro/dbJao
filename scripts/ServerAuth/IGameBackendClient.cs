using System.Threading;
using System.Threading.Tasks;

public interface IGameBackendClient
{
    Task<GameSessionValidationResult> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    Task<bool> SaveCharacterStateAsync(
        AuthenticatedPlayerState state,
        CancellationToken cancellationToken = default);
}

public sealed record GameSessionValidationResult(
    bool Valid,
    string? ErrorCode = null,
    AuthenticatedCharacterData? Character = null);
