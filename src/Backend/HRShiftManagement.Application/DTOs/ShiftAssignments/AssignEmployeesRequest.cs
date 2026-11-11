using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace HRShiftManagement.Application.DTOs.ShiftAssignments;

public class AssignEmployeesRequest
{
    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    public DateOnly WorkDate { get; set; }

    [Required]
    [MinLength(1, ErrorMessage =
        "Phải chọn ít nhất một nhân viên.")]
    public List<Guid> EmployeeIds { get; set; } = new();
}
