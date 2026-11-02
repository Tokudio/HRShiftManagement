namespace HRShiftManagement.Infrastructure.Configurations;

/// <summary>
/// Strongly typed configuration options for JWT settings.
/// Shared across Infrastructure and API layers.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;
    public string SecretKey { get; set; } = string.Empty;
}
