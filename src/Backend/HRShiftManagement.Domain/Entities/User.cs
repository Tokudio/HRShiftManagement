using HRShiftManagement.Domain.Common;
using HRShiftManagement.Domain.Enums;

namespace HRShiftManagement.Domain.Entities;

/// <summary>
/// Represents a user account in the system.
/// Email is the unique login identifier.
/// Each user has exactly one role.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}