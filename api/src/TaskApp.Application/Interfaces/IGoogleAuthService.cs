using Google.Apis.Auth;

namespace TaskApp.Application.Interfaces;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(string idToken);
}
