using HRShiftManagement.Domain.Common;
using HRShiftManagement.Domain.Enums;

namespace HRShiftManagement.Domain.Entities;

// Ho so nhan vien
public class EmployeeProfile : BaseEntity
{
    // Tai khoan dang nhap lien ket (neu co)
    public Guid? UserId { get; set; }

    public User? User { get; set; }

    // Ma nhan vien
    public string EmployeeCode { get; set; } = string.Empty;

    // Ho va ten
    public string FullName { get; set; } = string.Empty;

    // So dien thoai
    public string PhoneNumber { get; set; } = string.Empty;

    // Ngay sinh
    public DateOnly DateOfBirth { get; set; }

    // Chuc vu
    public string Position { get; set; } = string.Empty;

    // Phong ban
    public string Department { get; set; } = string.Empty;

    // Thong tin bo sung
    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? JoinedOn { get; set; }

    public string? Notes { get; set; }

    // Trang thai nhan vien
    public UserStatus Status { get; set; } = UserStatus.Active;
}
