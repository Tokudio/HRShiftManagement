using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.DTOs.ShiftAssignments;

public class EmployeeScheduleResponse
{
    public DateOnly WorkDate { get; set; }

    public string ShiftName { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}
