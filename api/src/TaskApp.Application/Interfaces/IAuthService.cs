using TaskApp.Application.Common;
using TaskApp.Application.Dtos;

namespace TaskApp.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken ct = default);
}
