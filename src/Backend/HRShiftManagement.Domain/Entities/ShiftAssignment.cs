using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using HRShiftManagement.Domain.Common;

namespace HRShiftManagement.Domain.Entities;

public class ShiftAssignment : BaseEntity
{
    public Guid ShiftId { get; set; }

    public Shift Shift { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public User Employee { get; set; } = null!;

    public DateOnly WorkDate { get; set; }
}
