using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.Employee;

public class EmployeeRequest
{
    [Required(ErrorMessage = "Mã nhân viên không được để trống.")]
    [StringLength(30)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [RegularExpression(
        @"^(0[0-9]{9}|\+84[0-9]{9})$",
        ErrorMessage = "Số điện thoại không hợp lệ."
    )]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày sinh không được để trống.")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Chức vụ không được để trống.")]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phòng ban không được để trống.")]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;
}
