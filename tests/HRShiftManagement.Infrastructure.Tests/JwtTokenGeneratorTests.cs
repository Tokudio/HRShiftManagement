using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Configurations;
using HRShiftManagement.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "HRShiftManagementTest",
        Audience = "HRShiftManagementTestAudience",
        ExpirationInMinutes = 60,
        SecretKey = "SuperSecretKeyForTestingJwtTokenGeneratorMustBeAtLeast32BytesLong!"
    };

    private JwtTokenGenerator CreateGenerator()
    {
        var optionsMock = Options.Create(_options);
        return new JwtTokenGenerator(optionsMock);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt_WithExpectedClaims()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@example.com",
            FullName = "John Doe",
            Role = UserRole.Manager,
            Status = UserStatus.Active
        };

        // Act
        string token = generator.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(_options.Issuer, jwtToken.Issuer);
        Assert.Contains(_options.Audience, jwtToken.Audiences);

        // Verify claims
        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Role.ToString(), jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(user.FullName, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnCryptographicallySecureNonEmptyString()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        string refreshToken1 = generator.GenerateRefreshToken();
        string refreshToken2 = generator.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(refreshToken1));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken2));
        Assert.NotEqual(refreshToken1, refreshToken2); // Extremely unlikely to collide

        // 64 bytes base64-encoded is 88 characters
        byte[] decoded = Convert.FromBase64String(refreshToken1);
        Assert.Equal(64, decoded.Length);
    }
}
