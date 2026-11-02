using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Auth;

namespace HRShiftManagement.Application.Interfaces;

/// <summary>
/// Use case service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account with default Employee role.
    /// Validates email uniqueness and password confirmation.
    /// </summary>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user by email and password, checking account active status.
    /// Returns access and refresh tokens upon success.
    /// </summary>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and issues a new access token and rotated refresh token.
    /// </summary>
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an existing refresh token (used during logout).
    /// If userId is specified, ensures the token belongs to that user.
    /// </summary>
    Task<Result<bool>> RevokeTokenAsync(string refreshToken, Guid? userId = null, CancellationToken cancellationToken = default);
}
