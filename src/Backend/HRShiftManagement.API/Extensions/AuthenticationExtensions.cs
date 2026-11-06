
using System.Security.Claims;
using System.Text;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Configurations;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HRShiftManagement.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        // Validate required configuration values
        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey is missing. Please configure 'JwtSettings:SecretKey' via User Secrets in Development " +
                "(run: dotnet user-secrets set \"JwtSettings:SecretKey\" \"<your-at-least-32-char-key>\" --project src/Backend/HRShiftManagement.API) " +
                "or via environment variable 'JwtSettings__SecretKey' in Production.");
        }

        if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 256 bits (32 bytes) long for secure HMAC-SHA256 signing.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is missing. Please configure 'JwtSettings:Issuer' in appsettings.json or environment variables.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException(
                "JWT Audience is missing. Please configure 'JwtSettings:Audience' in appsettings.json or environment variables.");
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.Configure<JwtOptions>(jwtSection);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Check the user's current account status and role in the database.
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdValue = context.Principal?
                        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var tokenRole = context.Principal?
                        .FindFirst(ClaimTypes.Role)?.Value;

                    // Reject tokens without a valid identity or role.
                    if (!Guid.TryParse(userIdValue, out var userId) ||
                        string.IsNullOrWhiteSpace(tokenRole))
                    {
                        context.Fail("Invalid user identity or role in token.");
                        return;
                    }

                    // Resolve the existing DbContext from Dependency Injection.
                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<ApplicationDbContext>();

                    var currentUser = await dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new
                        {
                            u.Role,
                            u.Status
                        })
                        .FirstOrDefaultAsync(
                            context.HttpContext.RequestAborted);

                    // Reject tokens belonging to deleted or unknown users.
                    if (currentUser == null)
                    {
                        context.Fail("User does not exist.");
                        return;
                    }

                    // Only active accounts can authenticate.
                    if (currentUser.Status != UserStatus.Active)
                    {
                        context.Fail("User account is not active.");
                        return;
                    }

                    // Reject tokens whose role no longer matches the database.
                    if (!string.Equals(
                        tokenRole,
                        currentUser.Role.ToString(),
                        StringComparison.Ordinal))
                    {
                        context.Fail("User role has changed. Please log in again.");
                    }
                }
            };
        });

        return services;
    }
}
