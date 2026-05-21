namespace TaskApp.Application.Dtos;

public record LoginDto(string Email, string Password);
public record RegisterDto(string Email, string Password, string FullName);
public record AuthResponseDto(string Token, string Email, string FullName, Guid UserId);