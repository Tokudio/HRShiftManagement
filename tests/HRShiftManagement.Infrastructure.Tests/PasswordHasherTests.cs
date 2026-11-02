using HRShiftManagement.Infrastructure.Services;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashes_ForSamePassword()
    {
        // Arrange
        const string password = "SecurePassword123!";

        // Act
        string hash1 = _hasher.HashPassword(password);
        string hash2 = _hasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        Assert.NotEqual(hash1, hash2); // Due to distinct random salts
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordIsCorrect()
    {
        // Arrange
        const string password = "MyP@ssw0rd!2026";
        string hash = _hasher.HashPassword(password);

        // Act
        bool isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        // Arrange
        const string correctPassword = "CorrectP@ssw0rd!";
        const string wrongPassword = "WrongP@ssw0rd!";
        string hash = _hasher.HashPassword(correctPassword);

        // Act
        bool isValid = _hasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-hash-string")]
    [InlineData("100000.invalidbase64.invalidbase64")]
    public void VerifyPassword_ShouldReturnFalse_WhenHashIsMalformed(string invalidHash)
    {
        // Act
        bool isValid = _hasher.VerifyPassword("SomePassword", invalidHash);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsEmpty(string? emptyPassword)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _hasher.HashPassword(emptyPassword!));
    }
}
