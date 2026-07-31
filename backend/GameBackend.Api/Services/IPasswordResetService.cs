using GameBackend.Api.DTOs.Authentication;

namespace GameBackend.Api.Services;

public interface IPasswordResetService
{
    Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken);
    Task<ResetPasswordResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken);
}
