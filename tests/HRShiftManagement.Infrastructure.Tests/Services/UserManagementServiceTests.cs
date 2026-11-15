
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using HRShiftManagement.Application.DTOs.Users;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class UserManagementServiceTests
{
    private static User CreateUser(
        string fullName,
        string email,
        UserRole role = UserRole.Employee)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = "TestPasswordHash",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
    private static CreateUserRequest CreateValidCreateUserRequest()
    {
        return new CreateUserRequest
        {
            FullName = "Test User",
            Email = "testuser@example.com",
            PhoneNumber = "0901234567",
            Password = "SecurePassword123!",
            Role = "Employee"
        };
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUserAsync_WithEmptyFullName_ShouldFail(
    string fullName)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.FullName = fullName;

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Full name is required.", result.ErrorMessage);
        Assert.Empty(fixture.Context.Users);
    }
    [Fact]
    public async Task CreateUserAsync_WithFullNameExceedingLimit_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.FullName = new string('A', 201);

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Full name cannot exceed 200 characters.",
            result.ErrorMessage);

        Assert.Empty(fixture.Context.Users);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUserAsync_WithEmptyEmail_ShouldFail(
    string email)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.Email = email;

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Email is required.", result.ErrorMessage);
        Assert.Empty(fixture.Context.Users);
    }
    [Fact]
    public async Task CreateUserAsync_WithInvalidEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.Email = "invalid-email";

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Invalid email address format.",
            result.ErrorMessage);

        Assert.Empty(fixture.Context.Users);
    }
    [Fact]
    public async Task CreateUserAsync_WithEmptyPassword_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.Password = "";

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            "Password must be at least 8 characters long",
            result.ErrorMessage);

        Assert.Empty(fixture.Context.Users);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]

    public async Task CreateUserAsync_WithEmptyRole_ShouldFail(
    string role)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.Role = role;

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid role.", result.ErrorMessage);
        Assert.Empty(fixture.Context.Users);
    }
    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("99")]
    public async Task CreateUserAsync_WithInvalidRole_ShouldFail(
    string role)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.Role = role;

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid role.", result.ErrorMessage);
        Assert.Empty(fixture.Context.Users);
    }
    [Fact]
    public async Task CreateUserAsync_WithPhoneNumberExceedingLimit_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new UserManagementService(
            fixture.Context,
            new PasswordHasher());

        var request = CreateValidCreateUserRequest();
        request.PhoneNumber = new string('1', 21);

        var result = await service.CreateUserAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Phone number cannot exceed 20 characters.",
            result.ErrorMessage);

        Assert.Empty(fixture.Context.Users);
    }
    [Fact]
    public async Task GetUsersAsync_WithExistingUsers_ShouldReturnAllUsers()
    {
        // Arrange
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var user1 = CreateUser(
            "Nguyen Van A",
            "vana@example.com");

        var user2 = CreateUser(
            "Tran Thi B",
            "thib@example.com",
            UserRole.Manager);

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        Assert.Contains(
            result.Data,
            u => u.Id == user1.Id);

        Assert.Contains(
            result.Data,
            u => u.Id == user2.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnUsersOrderedByFullName()
    {
        // Arrange
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var user1 = CreateUser(
            "Tran Van C",
            "vanc@example.com");

        var user2 = CreateUser(
            "Nguyen Van A",
            "vana@example.com");

        var user3 = CreateUser(
            "Le Thi B",
            "thib@example.com");

        context.Users.AddRange(user1, user2, user3);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Count);

        Assert.Equal(
            new[] { "Le Thi B", "Nguyen Van A", "Tran Van C" },
            result.Data.Select(u => u.FullName).ToArray());
    }
    private static RefreshToken CreateRefreshToken(
    Guid userId,
    string tokenHash,
    bool isRevoked = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            RevokedAt = isRevoked ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }
    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new CreateUserRequest
        {
            FullName = "  Nguyen Van A  ",
            Email = "  NGUYENVANA@example.com  ",
            PhoneNumber = " 0901234567 ",
            Password = "SecurePassword123!",
            Role = "Employee"
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal("Nguyen Van A", result.Data.FullName);
        Assert.Equal("nguyenvana@example.com", result.Data.Email);
        Assert.Equal("0901234567", result.Data.PhoneNumber);
        Assert.Equal("Employee", result.Data.Role);
        Assert.Equal("Active", result.Data.Status);

        // Verify user was saved to Database
        var savedUser = await context.Users
            .FindAsync(result.Data.Id);

        Assert.NotNull(savedUser);
        Assert.NotEqual(
            request.Password,
            savedUser.PasswordHash);

        // Verify password was hashed correctly
        var passwordHasher = new PasswordHasher();

        Assert.True(
            passwordHasher.VerifyPassword(
                request.Password,
                savedUser.PasswordHash));
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        context.Users.Add(CreateUser(
            "Existing User",
            "existing@example.com"));

        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new CreateUserRequest
        {
            FullName = "New User",
            Email = "EXISTING@example.com",
            Password = "SecurePassword123!",
            Role = "Employee"
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(
            "An account with this email already exists.",
            result.ErrorMessage);

        Assert.Equal(1, context.Users.Count());
    }

    [Fact]
    public async Task CreateUserAsync_WithWeakPassword_ShouldFail()
    {
        // Arrange
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new CreateUserRequest
        {
            FullName = "Weak Password User",
            Email = "weakpassword@example.com",
            Password = "weakpassword",
            Role = "Employee"
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(
            "Password must be at least 8 characters long",
            result.ErrorMessage);

        Assert.Empty(context.Users);
    }
    [Fact]
    public async Task UpdateUserAsync_WithValidData_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var user = CreateUser("Old Name", "old@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new UpdateUserRequest
        {
            FullName = "  Nguyen Van A  ",
            Email = "  NEW@example.com  ",
            PhoneNumber = " 0901234567 "
        };

        var result = await service.UpdateUserAsync(user.Id, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal("Nguyen Van A", result.Data.FullName);
        Assert.Equal("new@example.com", result.Data.Email);
        Assert.Equal("0901234567", result.Data.PhoneNumber);

        var updatedUser = await context.Users.FindAsync(user.Id);

        Assert.NotNull(updatedUser);
        Assert.Equal("Nguyen Van A", updatedUser.FullName);
        Assert.Equal("new@example.com", updatedUser.Email);
        Assert.Equal("0901234567", updatedUser.PhoneNumber);
    }
    [Fact]
    public async Task UpdateUserAsync_WithDuplicateEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var user1 = CreateUser("User One", "user1@example.com");
        var user2 = CreateUser("User Two", "user2@example.com");

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new UpdateUserRequest
        {
            FullName = "Updated User",
            Email = "USER2@example.com",
            PhoneNumber = "0901234567"
        };

        var result = await service.UpdateUserAsync(user1.Id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "An account with this email already exists.",
            result.ErrorMessage);

        var unchangedUser = await context.Users.FindAsync(user1.Id);

        Assert.NotNull(unchangedUser);
        Assert.Equal("User One", unchangedUser.FullName);
        Assert.Equal("user1@example.com", unchangedUser.Email);
    }
    [Fact]
    public async Task UpdateUserAsync_WithNonExistingUser_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new UpdateUserRequest
        {
            FullName = "New Name",
            Email = "new@example.com",
            PhoneNumber = "0901234567"
        };

        var nonExistingUserId = Guid.NewGuid();

        var result = await service.UpdateUserAsync(
            nonExistingUserId,
            request);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found.", result.ErrorMessage);

        Assert.Empty(context.Users);
    }
    [Fact]
    public async Task UpdateUserAsync_WithSameEmail_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var user = CreateUser("Old Name", "user@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var request = new UpdateUserRequest
        {
            FullName = "Updated Name",
            Email = "USER@example.com",
            PhoneNumber = "0901234567"
        };

        var result = await service.UpdateUserAsync(user.Id, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal("Updated Name", result.Data.FullName);
        Assert.Equal("user@example.com", result.Data.Email);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithValidAdmin_ShouldUpdateRoleAndRevokeTokens()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        var employee = CreateUser("Employee", "employee@example.com");

        var activeToken = CreateRefreshToken(
            employee.Id, "active-token-hash");

        var revokedToken = CreateRefreshToken(
            employee.Id, "revoked-token-hash", true);

        context.Users.AddRange(admin, employee);
        context.RefreshTokens.AddRange(activeToken, revokedToken);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            employee.Id,
            "Manager",
            admin.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        var updatedUser = await context.Users.FindAsync(employee.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(UserRole.Manager, updatedUser.Role);

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == employee.Id)
            .ToListAsync();

        Assert.All(tokens, token => Assert.True(token.IsRevoked));
        Assert.NotNull(activeToken.RevokedAt);
        Assert.Equal(revokedToken.RevokedAt, tokens
            .Single(t => t.Id == revokedToken.Id).RevokedAt);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithNonAdminActor_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var employee = CreateUser("Employee", "employee@example.com");
        var target = CreateUser("Target", "target@example.com");

        context.Users.AddRange(employee, target);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            target.Id,
            "Manager",
            employee.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Only administrators can change user roles.",
            result.ErrorMessage);

        Assert.Equal(UserRole.Employee, target.Role);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WhenChangingOwnRole_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            admin.Id,
            "Employee",
            admin.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "You cannot change your own role.",
            result.ErrorMessage);

        Assert.Equal(UserRole.Admin, admin.Role);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithNonExistingUser_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            Guid.NewGuid(),
            "Manager",
            admin.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found.", result.ErrorMessage);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithSameRole_ShouldSucceedWithoutRevokingTokens()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        var employee = CreateUser("Employee", "employee@example.com");

        var token = CreateRefreshToken(
            employee.Id, "active-token-hash");

        context.Users.AddRange(admin, employee);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            employee.Id,
            "Employee",
            admin.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        Assert.Equal(UserRole.Employee, employee.Role);
        Assert.False(token.IsRevoked);
        Assert.Null(token.RevokedAt);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithRoleContainingWhitespace_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        var employee = CreateUser("Employee", "employee@example.com");

        context.Users.AddRange(admin, employee);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            employee.Id,
            "  mAnAgEr  ",
            admin.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Manager, employee.Role);
    }
    [Fact]
    public async Task UpdateUserRoleAsync_WithInvalidRole_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        context.Users.AddRange(admin, employee);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserRoleAsync(
            employee.Id,
            "SuperAdmin",
            admin.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid role.", result.ErrorMessage);
        Assert.Equal(UserRole.Employee, employee.Role);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithValidAdmin_ShouldLockUserAndRevokeTokens()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        var activeToken = CreateRefreshToken(
            employee.Id,
            "active-token-hash");

        var revokedToken = CreateRefreshToken(
            employee.Id,
            "revoked-token-hash",
            isRevoked: true);

        context.Users.AddRange(admin, employee);

        context.RefreshTokens.AddRange(
            activeToken,
            revokedToken);

        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            employee.Id,
            "Locked",
            admin.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            UserStatus.Locked,
            employee.Status);

        Assert.True(activeToken.IsRevoked);
        Assert.NotNull(activeToken.RevokedAt);

        Assert.True(revokedToken.IsRevoked);
        Assert.NotNull(revokedToken.RevokedAt);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithNonAdminActor_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        var target = CreateUser(
            "Target",
            "target@example.com");

        context.Users.AddRange(employee, target);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            target.Id,
            "Locked",
            employee.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "Only active administrators can change user status.",
            result.ErrorMessage);

        Assert.Equal(
            UserStatus.Active,
            target.Status);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithLockedAdminActor_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        admin.Status = UserStatus.Locked;

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        context.Users.AddRange(admin, employee);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            employee.Id,
            "Locked",
            admin.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "Only active administrators can change user status.",
            result.ErrorMessage);

        Assert.Equal(UserStatus.Active, employee.Status);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithNonExistingUser_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var nonExistingUserId = Guid.NewGuid();

        var result = await service.UpdateUserStatusAsync(
            nonExistingUserId,
            "Locked",
            admin.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "User not found.",
            result.ErrorMessage);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WhenAdminLocksOwnAccount_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            admin.Id,
            "Locked",
            admin.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "You cannot lock your own account.",
            result.ErrorMessage);

        Assert.Equal(UserStatus.Active, admin.Status);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithInvalidStatus_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        context.Users.AddRange(admin, employee);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            employee.Id,
            "Inactive",
            admin.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
    "Invalid status. Only Active or Locked is allowed.",
    result.ErrorMessage);

        Assert.Equal(UserStatus.Active, employee.Status);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithEmptyStatus_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        context.Users.AddRange(admin, employee);
        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            employee.Id,
            "   ",
            admin.Id);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            "Status is required.",
            result.ErrorMessage);

        Assert.Equal(UserStatus.Active, employee.Status);
    }
    [Fact]
    public async Task UpdateUserStatusAsync_WithValidAdmin_ShouldUnlockUser()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var context = fixture.Context;

        var admin = CreateUser(
            "Admin",
            "admin@example.com",
            UserRole.Admin);

        var employee = CreateUser(
            "Employee",
            "employee@example.com");

        employee.Status = UserStatus.Locked;

        var revokedToken = CreateRefreshToken(
            employee.Id,
            "revoked-token-hash",
            isRevoked: true);

        context.Users.AddRange(admin, employee);
        context.RefreshTokens.Add(revokedToken);

        await context.SaveChangesAsync();

        var service = new UserManagementService(
            context,
            new PasswordHasher());

        var result = await service.UpdateUserStatusAsync(
            employee.Id,
            "Active",
            admin.Id);

        Assert.True(result.IsSuccess);

        Assert.Equal(UserStatus.Active, employee.Status);

        Assert.True(revokedToken.IsRevoked);
        Assert.NotNull(revokedToken.RevokedAt);
    }
}
