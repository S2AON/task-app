using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskApp.Application.Dtos;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Data;
using TaskApp.Infrastructure.Repositories;
using TaskApp.Infrastructure.Services;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskApp.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _service;
    private readonly Mock<IGoogleAuthService> _googleAuthMock;
    private const string TestSecret = "test-super-secret-key-min-32-chars-long-for-security";
    private const string TestIssuer = "TestTaskApp";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var userRepository = new Repository<User>(_context);
        var unitOfWork = new UnitOfWork(_context);
        var jwtService = new JwtService(TestSecret, TestIssuer);
        _googleAuthMock = new Mock<IGoogleAuthService>();
        _service = new AuthService(userRepository, unitOfWork, jwtService, _googleAuthMock.Object);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldSucceed()
    {
        var dto = new RegisterDto("newuser@example.com", "SecurePass123!", "New User");

        var result = await _service.RegisterAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("newuser@example.com", result.Data.Email);
        Assert.NotEmpty(result.Data.Token);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        Assert.NotNull(userInDb);
        Assert.NotEqual(Guid.Empty, userInDb.Id);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldFail()
    {
        await _service.RegisterAsync(new RegisterDto("existing@example.com", "Pass123!", "User"));

        var result = await _service.RegisterAsync(new RegisterDto("existing@example.com", "OtherPass!", "Other"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Email already registered", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        const string plainPassword = "SecurePass123!";
        await _service.RegisterAsync(new RegisterDto("hash@example.com", plainPassword, "Hash User"));

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "hash@example.com");
        Assert.NotNull(user);
        Assert.NotEqual(plainPassword, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        await _service.RegisterAsync(new RegisterDto("login@example.com", "SecurePass123!", "Login User"));

        var result = await _service.LoginAsync(new LoginDto("login@example.com", "SecurePass123!"));

        Assert.True(result.IsSuccess);
        Assert.Equal("login@example.com", result.Data!.Email);
        Assert.NotEmpty(result.Data.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldFail()
    {
        await _service.RegisterAsync(new RegisterDto("wrong@example.com", "CorrectPass123!", "User"));

        var result = await _service.LoginAsync(new LoginDto("wrong@example.com", "WrongPass!"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistingEmail_ShouldFail()
    {
        var result = await _service.LoginAsync(new LoginDto("ghost@example.com", "AnyPass!"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials", result.Message);
    }

    // ── Google Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLoginAsync_WithValidToken_ShouldCreateNewUser()
    {
        _googleAuthMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new GoogleJsonWebSignature.Payload
            {
                Subject = "google-subject-123",
                Email = "google@example.com",
                Name = "Google User"
            });

        var result = await _service.GoogleLoginAsync(new GoogleLoginDto("valid-google-token"));

        Assert.True(result.IsSuccess);
        Assert.Equal("google@example.com", result.Data!.Email);
        Assert.NotEmpty(result.Data.Token);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "google@example.com");
        Assert.NotNull(userInDb);
        Assert.Equal("google-subject-123", userInDb.GoogleId);
        Assert.Null(userInDb.PasswordHash);
    }

    [Fact]
    public async Task GoogleLoginAsync_WithExistingGoogleUser_ShouldNotCreateDuplicate()
    {
        _googleAuthMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new GoogleJsonWebSignature.Payload
            {
                Subject = "google-subject-456",
                Email = "returning@example.com",
                Name = "Returning User"
            });

        await _service.GoogleLoginAsync(new GoogleLoginDto("token"));
        await _service.GoogleLoginAsync(new GoogleLoginDto("token"));

        var count = await _context.Users.CountAsync(u => u.Email == "returning@example.com");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GoogleLoginAsync_WithExistingLocalAccount_ShouldLinkGoogleId()
    {
        await _service.RegisterAsync(new RegisterDto("local@example.com", "Pass123!", "Local User"));

        _googleAuthMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(new GoogleJsonWebSignature.Payload
            {
                Subject = "google-subject-789",
                Email = "local@example.com",
                Name = "Local User"
            });

        var result = await _service.GoogleLoginAsync(new GoogleLoginDto("token"));

        Assert.True(result.IsSuccess);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "local@example.com");
        Assert.NotNull(user);
        Assert.Equal("google-subject-789", user.GoogleId);
        Assert.NotNull(user.PasswordHash);
    }

    [Fact]
    public async Task GoogleLoginAsync_WithInvalidToken_ShouldFail()
    {
        _googleAuthMock
            .Setup(g => g.ValidateTokenAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid Google token"));

        var result = await _service.GoogleLoginAsync(new GoogleLoginDto("invalid-token"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid Google token", result.Message);
    }

    // ── JWT format ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_TokenShouldBeValidJwt()
    {
        var result = await _service.RegisterAsync(new RegisterDto("jwt@example.com", "SecurePass123!", "JWT User"));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Token.Split('.').Length);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
