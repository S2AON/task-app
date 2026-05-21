using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskApp.Application.Interfaces;

namespace TaskApp.Infrastructure.Services;

public class GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [configuration["GoogleAuth:ClientId"]]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            logger.LogInformation("Google token validated for {Email}", payload.Email);
            return payload;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google token validation failed");
            throw new UnauthorizedAccessException("Invalid Google token");
        }
    }
}
