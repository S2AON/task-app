using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using TaskApp.Infrastructure.Services;

namespace TaskApp.Api.Middleware;

public static class AuthMiddleware
{
    public static ClaimsPrincipal? ValidateRequest(HttpRequestData req, JwtService jwtService)
    {
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            return null;
        }

        var authHeader = authHeaders.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        return jwtService.ValidateToken(token);
    }

    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }
}
