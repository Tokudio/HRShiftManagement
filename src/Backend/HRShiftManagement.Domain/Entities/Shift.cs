using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using HRShiftManagement.Domain.Common;

namespace HRShiftManagement.Domain.Entities;

public class Shift : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int MinEmployees { get; set; }

    public int MaxEmployees { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; }
        = new List<ShiftAssignment>();
}
