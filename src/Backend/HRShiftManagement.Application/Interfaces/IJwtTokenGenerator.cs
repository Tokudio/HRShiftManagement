using HRShiftManagement.Domain.Entities;

namespace HRShiftManagement.Application.Interfaces;

/// <summary>
/// Service abstraction for generating JWT access tokens and cryptographically secure refresh tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT access token containing standard claims for the given user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically secure random refresh token string.
    /// </summary>
    string GenerateRefreshToken();
}
