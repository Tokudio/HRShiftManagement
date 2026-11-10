
using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.Users;

public class UpdateUserStatusRequest
{
    [Required(ErrorMessage = "Trạng thái không được để trống.")]
    public string Status { get; set; } = string.Empty;
}
