namespace HRShiftManagement.Application.Interfaces;

/// <summary>
/// Service abstraction for securely hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using a secure hashing algorithm.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored password hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
