using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.Employees;

public class EmployeeProfileRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(200, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [RegularExpression(
        @"^0[0-9]{9}$",
        ErrorMessage = "Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0."
    )]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Chức vụ không được để trống.")]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phòng ban không được để trống.")]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public DateOnly? JoinedOn { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
