using Google.Apis.Auth;
using TaskApp.Application.Common;
using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Entities;

namespace TaskApp.Infrastructure.Services;

public class AuthService(
    IRepository<User> userRepository,
    IUnitOfWork unitOfWork,
    JwtService jwtService,
    IGoogleAuthService googleAuthService) : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var emailTaken = await userRepository.ExistsAsync(u => u.Email == dto.Email, ct);
        if (emailTaken)
            return Result<AuthResponseDto>.Failure("Email already registered");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName
        };

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<AuthResponseDto>.Success(ToResponse(user, jwtService.GenerateToken(user)));
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var matches = await userRepository.FindAsync(u => u.Email == dto.Email, ct);
        var user = matches.FirstOrDefault();

        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure("Invalid credentials");

        return Result<AuthResponseDto>.Success(ToResponse(user, jwtService.GenerateToken(user)));
    }

    public async Task<Result<AuthResponseDto>> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await googleAuthService.ValidateTokenAsync(dto.IdToken);
        }
        catch
        {
            return Result<AuthResponseDto>.Failure("Invalid Google token");
        }

        // Look up by GoogleId first (fastest path for returning users)
        var byGoogleId = await userRepository.FindAsync(u => u.GoogleId == payload.Subject, ct);
        var user = byGoogleId.FirstOrDefault();

        if (user is null)
        {
            // Might be an existing local account — link it
            var byEmail = await userRepository.FindAsync(u => u.Email == payload.Email, ct);
            user = byEmail.FirstOrDefault();

            if (user is null)
            {
                user = new User
                {
                    Email = payload.Email,
                    FullName = payload.Name ?? payload.Email,
                    GoogleId = payload.Subject,
                    IsActive = true
                };
                await userRepository.AddAsync(user, ct);
            }
            else
            {
                user.GoogleId = payload.Subject;
                await userRepository.UpdateAsync(user, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result<AuthResponseDto>.Success(ToResponse(user, jwtService.GenerateToken(user)));
    }

    private static AuthResponseDto ToResponse(User user, string token) =>
        new(token, user.Email, user.FullName, user.Id);
}
