using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.DTOs.Shifts;

public class ShiftResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int MinEmployees { get; set; }

    public int MaxEmployees { get; set; }

    public bool CanDelete { get; set; }
}
