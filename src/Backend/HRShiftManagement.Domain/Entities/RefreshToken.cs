using HRShiftManagement.Domain.Common;

namespace HRShiftManagement.Domain.Entities;

/// <summary>
/// Represents a refresh token issued to a user.
/// The actual token value is never stored — only its SHA256 hash (TokenHash).
/// </summary>
public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}