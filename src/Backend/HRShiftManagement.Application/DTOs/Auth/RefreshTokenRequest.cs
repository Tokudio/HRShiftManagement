namespace HRShiftManagement.Application.DTOs.Auth;

/// <summary>
/// Data transfer object for requesting a new token pair using a refresh token.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

