using System.Security.Cryptography;
using System.Text;
using HRShiftManagement.Application.DTOs.Auth;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Configurations;
using HRShiftManagement.Infrastructure.Persistence.Context;
using HRShiftManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup In-Memory database for isolated testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _passwordHasher = new PasswordHasher();

        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "HRShiftManagementTest",
            Audience = "HRShiftManagementTestAudience",
            ExpirationInMinutes = 60,
            SecretKey = "SuperSecretKeyForTestingJwtTokenGeneratorMustBeAtLeast32BytesLong!"
        });

        _jwtTokenGenerator = new JwtTokenGenerator(_jwtOptions);

        _authService = new AuthService(
            _context,
            _passwordHasher,
            _jwtTokenGenerator,
            _jwtOptions);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldSucceed_AndAssignEmployeeRoleAndActiveStatus()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "nguyen.vana@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            PhoneNumber = "0901234567"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Nguyen Van A", result.Data.User.FullName);
        Assert.Equal("nguyen.vana@example.com", result.Data.User.Email);
        Assert.Equal(UserRole.Employee.ToString(), result.Data.User.Role);

        // Verify entity in DB
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "nguyen.vana@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.Employee, savedUser.Role);
        Assert.Equal(UserStatus.Active, savedUser.Status);
        Assert.NotEqual("SecurePassword123!", savedUser.PasswordHash); // Never stored as plaintext
        Assert.True(_passwordHasher.VerifyPassword("SecurePassword123!", savedUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotReturnTokens_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Tran Thi B",
            Email = "tranthib@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.AccessToken);
        Assert.Empty(result.Data.RefreshToken);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "duplicate@example.com",
            PasswordHash = _passwordHasher.HashPassword("Password123!"),
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            FullName = "New User",
            Email = "DUPLICATE@example.com", // Test case-insensitive normalization
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("An account with this email already exists.", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Mismatch User",
            Email = "mismatch@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Password and Confirm Password do not match.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("short")] // Less than 8 characters
    [InlineData("nouppercase123!")] // No uppercase
    [InlineData("NOLOWERCASE123!")] // No lowercase
    [InlineData("NoDigitsHere!")] // No digit
    [InlineData("NoSpecialChar123")] // No special character
    public async Task RegisterAsync_WithWeakPassword_ShouldFailPolicyCheck(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequest
        {
            FullName = "Weak Pass User",
            Email = "weakpass@example.com",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Password must be at least 8 characters long", result.ErrorMessage);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed_AndReturnTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Active User",
            Email = "active.user@example.com",
            PasswordHash = _passwordHasher.HashPassword("CorrectP@ssword123!"),
            Role = UserRole.Manager,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "ACTIVE.USER@example.com", // Case-insensitive login
            Password = "CorrectP@ssword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        Assert.Equal("Active User", result.Data.User.FullName);
        Assert.Equal(UserRole.Manager.ToString(), result.Data.User.Role);

        // Verify LastLoginAt updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser?.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldFailWithGenericMessage()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Target User",
            Email = "target@example.com",
            PasswordHash = _passwordHasher.HashPassword("RealP@ssword123!"),
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "target@example.com",
            Password = "WrongPassword999!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistingEmail_ShouldFailWithGenericMessage()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.ErrorMessage); // Does not reveal user existence
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsLocked_ShouldFailWithLockedMessage()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Locked User",
            Email = "locked@example.com",
            PasswordHash = _passwordHasher.HashPassword("RealP@ssword123!"),
            Role = UserRole.Employee,
            Status = UserStatus.Locked,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "locked@example.com",
            Password = "RealP@ssword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Account is locked. Please contact your administrator.", result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsInactive_ShouldFailWithInactiveMessage()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Inactive User",
            Email = "inactive@example.com",
            PasswordHash = _passwordHasher.HashPassword("RealP@ssword123!"),
            Role = UserRole.Employee,
            Status = UserStatus.Inactive,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "RealP@ssword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Account is inactive. Please contact support.", result.ErrorMessage);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task LoginAsync_ShouldStoreHashedRefreshToken_AndReturnPlaintextTokenToClient()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Token Test User",
            Email = "tokentest@example.com",
            PasswordHash = _passwordHasher.HashPassword("RealP@ssword123!"),
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "tokentest@example.com",
            Password = "RealP@ssword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        string plaintextRefreshToken = result.Data.RefreshToken;
        Assert.NotEmpty(plaintextRefreshToken);

        // Verify DB stored hash, NOT plaintext
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == user.Id);
        Assert.NotNull(storedToken);
        Assert.NotEqual(plaintextRefreshToken, storedToken.TokenHash); // Plaintext is NEVER stored
        Assert.False(storedToken.IsRevoked);

        // Compute expected SHA256 hash of plaintext token to verify accuracy
        byte[] expectedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintextRefreshToken));
        string expectedHash = Convert.ToBase64String(expectedHashBytes);
        Assert.Equal(expectedHash, storedToken.TokenHash);
    }

    #endregion
}
