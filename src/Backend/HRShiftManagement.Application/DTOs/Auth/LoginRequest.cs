namespace HRShiftManagement.Application.DTOs.Auth;

/// <summary>
/// Data transfer object for user login.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
