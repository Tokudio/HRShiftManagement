
using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.Users;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
    public string? PhoneNumber { get; set; }
}
