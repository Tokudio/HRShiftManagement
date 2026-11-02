using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRShiftManagement.Infrastructure.Services;

/// <summary>
/// Handles safe and idempotent bootstrapping of the initial system administrator account.
/// Reads credentials strictly from secure configuration (User Secrets / Environment Variables).
/// Never overwrites existing administrators or logs sensitive secrets.
/// </summary>
public class AdminBootstrapService : IAdminBootstrapService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminBootstrapService> _logger;

    // Minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        RegexOptions.Compiled);

    public AdminBootstrapService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AdminBootstrapService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAdminAsync(CancellationToken cancellationToken = default)
    {
        string? email = _configuration["AdminSetup:Email"];
        string? password = _configuration["AdminSetup:Password"];
        string? fullName = _configuration["AdminSetup:FullName"] ?? "System Administrator";
        string? phoneNumber = _configuration["AdminSetup:PhoneNumber"];

        // 1. If Admin credentials are not configured, skip safely without logging sensitive details
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Admin bootstrap skipped: AdminSetup:Email or AdminSetup:Password is not configured.");
            return;
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();

        // 2. Validate email format
        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            _logger.LogWarning("Admin bootstrap skipped: Invalid email format configured for AdminSetup:Email.");
            return;
        }

        // 3. Validate password complexity policy
        if (!PasswordRegex.IsMatch(password))
        {
            _logger.LogWarning("Admin bootstrap skipped: Configured Admin password does not satisfy security policy.");
            return;
        }

        // 4. Check if an Admin user already exists (idempotency check)
        bool adminExists = await _context.Users
            .AnyAsync(u => u.Role == UserRole.Admin, cancellationToken);

        if (adminExists)
        {
            _logger.LogInformation("Admin user already exists in the system. Admin bootstrap skipped.");
            return;
        }

        // 5. Check if another non-admin user already took this email
        bool emailTaken = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
        {
            _logger.LogWarning("Admin bootstrap cannot proceed: An account with email {Email} already exists with a different role.", normalizedEmail);
            return;
        }

        // 6. Create Admin user securely
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Initial administrator account created successfully for email: {Email}", normalizedEmail);
        }
        catch (DbUpdateException ex)
        {
            // Handles concurrent application instances booting up simultaneously
            _logger.LogWarning("Admin bootstrap completed or skipped due to concurrent creation: {Message}", ex.Message);
        }
    }
}

