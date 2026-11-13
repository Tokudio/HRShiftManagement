using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.DTOs.ShiftAssignments;

public class ScheduleCellResponse
{
    public DateOnly WorkDate { get; set; }

    public Guid ShiftId { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int MinEmployees { get; set; }

    public int MaxEmployees { get; set; }

    public int AssignedCount { get; set; }

    public List<EmployeeOptionResponse> Employees { get; set; }
        = new();
}
