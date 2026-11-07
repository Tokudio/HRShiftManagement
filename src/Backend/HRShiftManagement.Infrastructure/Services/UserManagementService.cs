
using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Users;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;

    public UserManagementService(ApplicationDbContext context)
    {
        _context = context;
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

        bool isValidRole = Enum.GetNames<UserRole>()
            .Any(r => string.Equals(
                r,
                normalizedRole,
                StringComparison.OrdinalIgnoreCase));

        if (!isValidRole ||
            !Enum.TryParse<UserRole>(
                normalizedRole,
                true,
                out var newRole))
        {
            return Result<bool>.Failure("Invalid role.");
        }

        // 2. Start transaction
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

        // 3. Find the Admin performing the operation
        var admin = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Id == performedByUserId,
                cancellationToken);

        if (admin == null || admin.Role != UserRole.Admin)
        {
            return Result<bool>.Failure(
                "Only administrators can change user roles.");
        }

        // 4. Prevent Admin from changing their own role
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

        // 6. Check if role is unchanged
        if (user.Role == newRole)
        {
            return Result<bool>.Success(true);
        }

        // 7. Prevent demoting the last Admin
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

        // 8. Update user role
        user.Role = newRole;

        // 9. Revoke all active refresh tokens of this user
        var refreshTokens = await _context.RefreshTokens
            .Where(r =>
                r.UserId == userId &&
                !r.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
        }

        // 10. Save changes
        await _context.SaveChangesAsync(cancellationToken);

        // 11. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
