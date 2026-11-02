namespace HRShiftManagement.Application.DTOs.Auth;

/// <summary>
/// Data transfer object for revoking a refresh token.
/// </summary>
public class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

