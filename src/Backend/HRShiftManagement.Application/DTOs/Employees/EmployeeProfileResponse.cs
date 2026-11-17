namespace HRShiftManagement.Application.DTOs.Employees;

public class EmployeeProfileResponse
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? JoinedOn { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsLinkedToAccount { get; set; }

    public bool IsProfileComplete { get; set; }
}
