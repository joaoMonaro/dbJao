using GameBackend.Api.DTOs.Authentication;

namespace GameBackend.Api.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    );
}
