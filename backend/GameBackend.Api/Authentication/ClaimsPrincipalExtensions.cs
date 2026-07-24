using System.Security.Claims;
using GameBackend.Api.Middleware;

namespace GameBackend.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out Guid userId))
            throw new UnauthorizedApiException("Token de acesso inválido.");

        return userId;
    }
}
