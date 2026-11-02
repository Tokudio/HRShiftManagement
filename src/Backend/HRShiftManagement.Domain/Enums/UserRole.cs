namespace HRShiftManagement.Domain.Enums;

/// <summary>
/// Defines the available roles in the system.
/// New roles can be added here without changing the database schema.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Employee = 3
}