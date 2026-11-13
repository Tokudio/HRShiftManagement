using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.DTOs.Users;

public class UserListItemDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
