namespace HRShiftManagement.Application.Interfaces;

/// <summary>
/// Service interface for bootstrapping the initial system administrator account.
/// </summary>
public interface IAdminBootstrapService
{
    /// <summary>
    /// Checks if an administrator account exists; if not, seeds one using secure configuration.
    /// Idempotent and concurrency-safe.
    /// </summary>
    Task SeedAdminAsync(CancellationToken cancellationToken = default);
}

