using HRShiftManagement.Application.DTOs.Employees;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class EmployeeProfileUpdateTests
{
    // =========================================
    // DU LIEU MAU
    // =========================================

    private static EmployeeProfileRequest ValidRequest(
        string email = "an@example.com")
    {
        return new EmployeeProfileRequest
        {
            FullName = "Nguyen Van An",
            PhoneNumber = "0912345678",
            DateOfBirth = new DateOnly(2002, 5, 15),
            Position = "Nhan vien ban hang",
            Department = "Kinh doanh",
            Email = email,
            Address = "Quy Nhon",
            JoinedOn = new DateOnly(2025, 1, 10),
            Notes = "Ho so ban dau"
        };
    }

    private static User NewEmployee(string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nhan vien cu",
            Email = email,
            PasswordHash = "test-hash",
            PhoneNumber = "0901111111",
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================
    // TEST 1: CAP NHAT HO SO THANH CONG
    // =========================================

    [Fact]
    public async Task UpdateAsync_ValidData_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var created = await service.CreateAsync(
            ValidRequest());

        var request = ValidRequest();

        request.FullName = "Nguyen Van An Moi";
        request.Position = "Truong nhom";
        request.Department = "Ban hang";
        request.PhoneNumber = "0987654321";

        var updated = await service.UpdateAsync(
            created.Id,
            request);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(
            created.EmployeeCode,
            updated.EmployeeCode);

        Assert.Equal("Nguyen Van An Moi", updated.FullName);
        Assert.Equal("Truong nhom", updated.Position);
        Assert.Equal("Ban hang", updated.Department);
        Assert.Equal("0987654321", updated.PhoneNumber);

        var saved = await fixture.Context.EmployeeProfiles
            .SingleAsync();

        Assert.Equal("Truong nhom", saved.Position);
        Assert.NotNull(saved.UpdatedAt);
    }

    // =========================================
    // TEST 2: KHONG TIM THAY HO SO
    // =========================================

    [Fact]
    public async Task UpdateAsync_UnknownId_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(
                Guid.NewGuid(),
                ValidRequest()));
    }

    // =========================================
    // TEST 3: SO DIEN THOAI KHONG HOP LE
    // =========================================

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("abcdefghij")]
    public async Task UpdateAsync_InvalidPhone_ShouldFail(
        string phone)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var created = await service.CreateAsync(
            ValidRequest());

        var request = ValidRequest();
        request.PhoneNumber = phone;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(
                created.Id,
                request));
    }

    // =========================================
    // TEST 4: EMAIL KHONG HOP LE
    // =========================================

    [Fact]
    public async Task UpdateAsync_InvalidEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var created = await service.CreateAsync(
            ValidRequest());

        var request = ValidRequest();
        request.Email = "email-sai";

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(
                created.Id,
                request));
    }

    // =========================================
    // TEST 5: KHONG DUOC TRUNG EMAIL HO SO KHAC
    // =========================================

    [Fact]
    public async Task UpdateAsync_DuplicateEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var first = await service.CreateAsync(
            ValidRequest("a@example.com"));

        var second = await service.CreateAsync(
            ValidRequest("b@example.com"));

        var request = ValidRequest("a@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                second.Id,
                request));

        var saved = await fixture.Context.EmployeeProfiles
            .AsNoTracking()
            .SingleAsync(p => p.Id == second.Id);

        Assert.Equal("b@example.com", saved.Email);
        Assert.NotEqual(first.Id, second.Id);
    }

    // =========================================
    // TEST 6: TAI KHOAN EMPLOYEE CU CHUA CO HO SO
    // =========================================

    [Fact]
    public async Task UpdateAsync_LegacyEmployee_ShouldCreateProfile()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var account = NewEmployee("legacy@example.com");

        fixture.Context.Users.Add(account);
        await fixture.Context.SaveChangesAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest("legacy@example.com");
        request.FullName = "Tran Van B";

        var result = await service.UpdateAsync(
            account.Id,
            request);

        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.Id, result.UserId);
        Assert.True(result.IsLinkedToAccount);
        Assert.True(result.IsProfileComplete);

        Assert.Equal(
            1,
            await fixture.Context.EmployeeProfiles.CountAsync());
    }

    // =========================================
    // TEST 7: CAP NHAT TAI KHOAN DA LIEN KET
    // =========================================

    [Fact]
    public async Task UpdateAsync_LinkedAccount_ShouldSyncUser()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var account = NewEmployee("an@example.com");

        fixture.Context.Users.Add(account);
        await fixture.Context.SaveChangesAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var profile = await service.CreateAsync(
            ValidRequest("an@example.com"));

        var request = ValidRequest("new@example.com");

        request.FullName = "Nguyen Van An Moi";
        request.PhoneNumber = "0987654321";

        var result = await service.UpdateAsync(
            profile.Id,
            request);

        Assert.True(result.IsLinkedToAccount);
        Assert.Equal("new@example.com", result.Email);

        var savedUser = await fixture.Context.Users
            .SingleAsync(u => u.Id == account.Id);

        Assert.Equal("new@example.com", savedUser.Email);
        Assert.Equal("Nguyen Van An Moi", savedUser.FullName);
        Assert.Equal("0987654321", savedUser.PhoneNumber);
    }

    // =========================================
    // TEST 8: LIEN KET HO SO VOI TAI KHOAN CO SAN
    // =========================================

    [Fact]
    public async Task UpdateAsync_UnlinkedProfile_ShouldLinkAccount()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        // Tao ho so chua co tai khoan
        var request = ValidRequest();
        request.Email = null;

        var profile = await service.CreateAsync(request);

        Assert.False(profile.IsLinkedToAccount);

        // Tao tai khoan Employee co email moi
        var account = NewEmployee("an@example.com");

        fixture.Context.Users.Add(account);
        await fixture.Context.SaveChangesAsync();

        // Cap nhat email ho so de lien ket
        request.Email = "an@example.com";

        var updated = await service.UpdateAsync(
            profile.Id,
            request);

        Assert.True(updated.IsLinkedToAccount);
        Assert.Equal(account.Id, updated.UserId);

        var saved = await fixture.Context.EmployeeProfiles
            .SingleAsync(p => p.Id == profile.Id);

        Assert.Equal(account.Id, saved.UserId);
    }

    // =========================================
    // TEST 9: TAI KHOAN LIEN KET PHAI CO EMAIL
    // =========================================

    [Fact]
    public async Task UpdateAsync_LinkedWithoutEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var account = NewEmployee("an@example.com");

        fixture.Context.Users.Add(account);
        await fixture.Context.SaveChangesAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var profile = await service.CreateAsync(
            ValidRequest("an@example.com"));

        var request = ValidRequest();
        request.Email = null;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(
                profile.Id,
                request));
    }
}
