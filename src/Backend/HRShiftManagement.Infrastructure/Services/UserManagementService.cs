using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Users;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HRShiftManagement.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        RegexOptions.Compiled);

    public UserManagementService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }


    public async Task<Result<List<UserListItemDto>>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.Status,
                u.PhoneNumber,
                u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var data = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role.ToString(),
            Status = u.Status.ToString(),
            PhoneNumber = u.PhoneNumber,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return Result<List<UserListItemDto>>.Success(data);
    }

    public async Task<Result<bool>> UpdateUserRoleAsync(
        Guid userId,
        string role,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate Role
        if (string.IsNullOrWhiteSpace(role))
        {
            return Result<bool>.Failure("Role is required.");
        }

        string normalizedRole = role.Trim();

        if (!Enum.TryParse<UserRole>(
                normalizedRole,
                true,
                out var newRole) ||
            !Enum.IsDefined(newRole))
        {
            return Result<bool>.Failure("Invalid role.");
        }

        // 2. Start transaction
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

        // 3. Verify Admin performing the operation
        var admin = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == performedByUserId,
                cancellationToken);

        if (admin == null || admin.Role != UserRole.Admin)
        {
            return Result<bool>.Failure(
                "Only administrators can change user roles.");
        }

        // 4. Prevent changing own role
        if (userId == performedByUserId)
        {
            return Result<bool>.Failure(
                "You cannot change your own role.");
        }

        // 5. Find target user
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.");
        }

        if (user.Role == newRole)
        {
            await transaction.CommitAsync(cancellationToken);
            return Result<bool>.Success(true);
        }

        // 6. Prevent demoting the last Admin
        if (user.Role == UserRole.Admin &&
            newRole != UserRole.Admin)
        {
            int adminCount = await _context.Users
                .CountAsync(
                    u => u.Role == UserRole.Admin,
                    cancellationToken);

            if (adminCount <= 1)
            {
                return Result<bool>.Failure(
                    "Cannot demote the last administrator.");
            }
        }

        // 7. Update role
        user.Role = newRole;

        // 8. Revoke active refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        // 9. Save and commit
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<UserListItemDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate FullName
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<UserListItemDto>.Failure(
                "Full name is required.");
        }

        string fullName = request.FullName.Trim();

        if (fullName.Length > 200)
        {
            return Result<UserListItemDto>.Failure(
                "Full name cannot exceed 200 characters.");
        }

        // 2. Validate and normalize Email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<UserListItemDto>.Failure(
                "Email is required.");
        }

        string email = request.Email.Trim().ToLowerInvariant();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return Result<UserListItemDto>.Failure(
                "Invalid email address format.");
        }

        // 3. Check duplicate email
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExists)
        {
            return Result<UserListItemDto>.Failure(
                "An account with this email already exists.");
        }

        // 4. Validate password using the same policy as registration
        if (string.IsNullOrWhiteSpace(request.Password) ||
            !PasswordRegex.IsMatch(request.Password))
        {
            return Result<UserListItemDto>.Failure(
                "Password must be at least 8 characters long and contain " +
                "at least one uppercase letter, one lowercase letter, " +
                "one digit, and one special character.");
        }

        // 5. Validate role
        if (string.IsNullOrWhiteSpace(request.Role) ||
            !Enum.TryParse<UserRole>(
                request.Role.Trim(),
                true,
                out var role) ||
            !Enum.IsDefined(role))
        {
            return Result<UserListItemDto>.Failure(
                "Invalid role.");
        }

        // 6. Validate phone number
        string? phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (phoneNumber?.Length > 20)
        {
            return Result<UserListItemDto>.Failure(
                "Phone number cannot exceed 20 characters.");
        }

        // 7. Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique Email index protects against concurrent requests.
            return Result<UserListItemDto>.Failure(
                "An account with this email already exists.");
        }

        // 8. Return safe user information
        var response = new UserListItemDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            PhoneNumber = user.PhoneNumber,
            LastLoginAt = user.LastLoginAt
        };

        return Result<UserListItemDto>.Success(response);
    }

    public async Task<Result<UserListItemDto>> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate FullName
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<UserListItemDto>.Failure(
                "Full name is required.");
        }

        string fullName = request.FullName.Trim();

        if (fullName.Length > 200)
        {
            return Result<UserListItemDto>.Failure(
                "Full name cannot exceed 200 characters.");
        }

        // 2. Validate and normalize Email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<UserListItemDto>.Failure(
                "Email is required.");
        }

        string email = request.Email.Trim().ToLowerInvariant();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            return Result<UserListItemDto>.Failure(
                "Invalid email address format.");
        }

        // 3. Validate PhoneNumber
        string? phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (phoneNumber?.Length > 20)
        {
            return Result<UserListItemDto>.Failure(
                "Phone number cannot exceed 20 characters.");
        }

        // 4. Find user
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken);

        if (user == null)
        {
            return Result<UserListItemDto>.Failure(
                "User not found.");
        }

        // 5. Check duplicate email, excluding current user
        bool emailExists = await _context.Users
            .AnyAsync(
                u => u.Email == email && u.Id != userId,
                cancellationToken);

        if (emailExists)
        {
            return Result<UserListItemDto>.Failure(
                "An account with this email already exists.");
        }

        // 6. Update allowed fields only
        user.FullName = fullName;
        user.Email = email;
        user.PhoneNumber = phoneNumber;

        // 7. Save changes
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<UserListItemDto>.Failure(
                "Unable to update user because of a database constraint.");
        }

        // 8. Return updated user information
        var response = new UserListItemDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            PhoneNumber = user.PhoneNumber,
            LastLoginAt = user.LastLoginAt
        };

        return Result<UserListItemDto>.Success(response);
    }
    public async Task<Result<bool>> UpdateUserStatusAsync(
    Guid userId,
    string status,
    Guid performedByUserId,
    CancellationToken cancellationToken = default)
    {
        // 1. Validate Status
        if (string.IsNullOrWhiteSpace(status))
        {
            return Result<bool>.Failure("Status is required.");
        }

        string normalizedStatus = status.Trim();

        if (!Enum.TryParse<UserStatus>(
                normalizedStatus,
                true,
                out var newStatus) ||
            !Enum.IsDefined(newStatus) ||
            (newStatus != UserStatus.Active &&
             newStatus != UserStatus.Locked))
        {
            return Result<bool>.Failure(
                "Invalid status. Only Active or Locked is allowed.");
        }

        // 2. Start transaction
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

        // 3. Verify Admin performing the operation
        var admin = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == performedByUserId,
                cancellationToken);

        if (admin == null ||
            admin.Role != UserRole.Admin ||
            admin.Status != UserStatus.Active)
        {
            return Result<bool>.Failure(
                "Only active administrators can change user status.");
        }

        // 4. Find target user
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.");
        }

        // 5. Prevent Admin from locking their own account
        if (userId == performedByUserId &&
            newStatus == UserStatus.Locked)
        {
            return Result<bool>.Failure(
                "You cannot lock your own account.");
        }

        // 6. Prevent locking the last active Admin
        if (user.Role == UserRole.Admin &&
            user.Status == UserStatus.Active &&
            newStatus == UserStatus.Locked)
        {
            int activeAdminCount = await _context.Users
                .CountAsync(
                    u => u.Role == UserRole.Admin &&
                         u.Status == UserStatus.Active,
                    cancellationToken);

            if (activeAdminCount <= 1)
            {
                return Result<bool>.Failure(
                    "Cannot lock the last active administrator.");
            }
        }

        // 7. Update user status
        user.Status = newStatus;

        // 8. Revoke active refresh tokens when locking the account
        if (newStatus == UserStatus.Locked)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync(cancellationToken);

            DateTime revokedAt = DateTime.UtcNow;

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = revokedAt;
            }
        }

        // 9. Save changes and commit transaction
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<bool>.Success(true);
    }


}