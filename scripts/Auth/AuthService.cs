using System.Threading;
using System.Threading.Tasks;

public sealed class AuthService(ApiClient apiClient)
{
    public Task<RegisterApiResponse> RegisterAsync(
        RegisterApiRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.RegisterAsync(request, cancellationToken);

    public Task<ForgotPasswordApiResponse> ForgotPasswordAsync(
        ForgotPasswordApiRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.ForgotPasswordAsync(request, cancellationToken);

    public Task<ResetPasswordApiResponse> ResetPasswordAsync(
        ResetPasswordApiRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.ResetPasswordAsync(request, cancellationToken);
}
