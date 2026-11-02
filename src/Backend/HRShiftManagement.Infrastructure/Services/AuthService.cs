using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Auth;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Configurations;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HRShiftManagement.Infrastructure.Services;

/// <summary>
/// Implements authentication use cases: user registration, login, token refresh, and token revocation.
/// Enforces password policy, secure hashing, and safe refresh token storage.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtOptions _jwtOptions;

    // Minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        RegexOptions.Compiled);

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate FullName
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<AuthResponse>.Failure("Full name is required.");
        }

        string trimmedFullName = request.FullName.Trim();
        if (trimmedFullName.Length > 200)
        {
            return Result<AuthResponse>.Failure("Full name cannot exceed 200 characters.");
        }

        // 2. Validate and normalize Email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<AuthResponse>.Failure("Email is required.");
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return Result<AuthResponse>.Failure("Invalid email address format.");
        }

        // 3. Validate Password and ConfirmPassword
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure("Password is required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Result<AuthResponse>.Failure("Password and Confirm Password do not match.");
        }

        // 4. Enforce Password Policy (>= 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char)
        if (!PasswordRegex.IsMatch(request.Password))
        {
            return Result<AuthResponse>.Failure(
                "Password must be at least 8 characters long and contain at least one uppercase letter, " +
                "one lowercase letter, one digit, and one special character.");
        }

        // 5. Check for duplicate email
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Result<AuthResponse>.Failure("An account with this email already exists.");
        }

        // 6. Hash password securely (never store plaintext or ConfirmPassword)
        string passwordHash = _passwordHasher.HashPassword(request.Password);

        // 7. Create User entity with default Role = Employee and Status = Active
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = trimmedFullName,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Safeguard against concurrent registration race conditions
            return Result<AuthResponse>.Failure("An account with this email already exists.");
        }

        // 8. Return created user info (automatic login is not performed after registration)
        var response = new AuthResponse
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = DateTime.MinValue,
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate inputs
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Find user by normalized email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // 3. Generic error message if user not found or password verification fails (prevents enumeration)
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        // 4. Check account status
        if (user.Status == UserStatus.Locked)
        {
            return Result<AuthResponse>.Failure("Account is locked. Please contact your administrator.");
        }

        if (user.Status == UserStatus.Inactive)
        {
            return Result<AuthResponse>.Failure("Account is inactive. Please contact support.");
        }

        // 5. Generate tokens
        string accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        // 6. Hash refresh token using SHA-256 before saving to database (never store plaintext)
        byte[] tokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        string tokenHash = Convert.ToBase64String(tokenHashBytes);

        // 7. Update LastLoginAt and persist new RefreshToken in the same transaction
        user.LastLoginAt = DateTime.UtcNow;

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiryDate = DateTime.UtcNow.AddDays(7), // 7 days expiration
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Return response with plaintext RefreshToken once to the client
        int expirationMinutes = _jwtOptions.ExpirationInMinutes > 0 ? _jwtOptions.ExpirationInMinutes : 60;
        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthResponse>.Failure("Refresh token is required.");
        }

        byte[] tokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken.Trim()));
        string tokenHash = Convert.ToBase64String(tokenHashBytes);

        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            return Result<AuthResponse>.Failure("Invalid refresh token.");
        }

        if (existingToken.IsRevoked)
        {
            return Result<AuthResponse>.Failure("Refresh token has been revoked.");
        }

        if (existingToken.ExpiryDate <= DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Refresh token has expired.");
        }

        var user = existingToken.User;
        if (user.Status == UserStatus.Locked)
        {
            return Result<AuthResponse>.Failure("Account is locked. Please contact your administrator.");
        }

        if (user.Status == UserStatus.Inactive)
        {
            return Result<AuthResponse>.Failure("Account is inactive. Please contact support.");
        }

        // Rotate token: revoke current token
        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;

        // Generate new token pair
        string newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        string newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        byte[] newTokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(newRefreshToken));
        string newTokenHash = Convert.ToBase64String(newTokenHashBytes);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        int expirationMinutes = _jwtOptions.ExpirationInMinutes > 0 ? _jwtOptions.ExpirationInMinutes : 60;
        var response = new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }
        };

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<bool>> RevokeTokenAsync(
        string refreshToken,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<bool>.Failure("Refresh token is required.");
        }

        byte[] tokenHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken.Trim()));
        string tokenHash = Convert.ToBase64String(tokenHashBytes);

        var existingToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            return Result<bool>.Failure("Invalid refresh token.");
        }

        if (existingToken.IsRevoked)
        {
            return Result<bool>.Failure("Refresh token has already been revoked.");
        }

        if (userId.HasValue && existingToken.UserId != userId.Value)
        {
            return Result<bool>.Failure("You are not authorized to revoke this token.");
        }

        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
