using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.Common;

public class ShiftConflictException : Exception
{
    public ShiftConflictException(string message)
        : base(message)
    {
    }
}
