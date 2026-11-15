using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRShiftManagement.Application.DTOs.Employee
{
    public class EmployeeResponse
    {
        public Guid Id { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Position { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
