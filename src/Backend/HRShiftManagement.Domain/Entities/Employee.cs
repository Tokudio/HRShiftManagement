using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRShiftManagement.Domain.Common;

namespace HRShiftManagement.Domain.Entities
{
    public class Employee : BaseEntity
    {
        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Position { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public Guid? UserId { get; set; }

        public User? User { get; set; }
    }
}
