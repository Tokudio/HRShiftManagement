using HRShiftManagement.Application.DTOs.Employees;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class EmployeeProfileServiceTests
{
    // =========================================
    // TAO DU LIEU HO SO HOP LE
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
            Notes = "Nhan vien moi"
        };
    }

    // =========================================
    // TAO TAI KHOAN NHAN VIEN MAU
    // =========================================

    private static User NewUser(
        string email,
        UserRole role = UserRole.Employee)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Tai khoan cu",
            Email = email,
            PasswordHash = "test-hash",
            PhoneNumber = "0901111111",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================
    // TEST 1: THEM HO SO THANH CONG
    // =========================================

    [Fact]
    public async Task CreateAsync_ValidData_ShouldSaveProfile()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest();

        request.FullName = "  Nguyen Van An  ";
        request.Email = "  AN@EXAMPLE.COM  ";
        request.PhoneNumber = " 0912345678 ";

        var result = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Nguyen Van An", result.FullName);
        Assert.Equal("0912345678", result.PhoneNumber);
        Assert.Equal("an@example.com", result.Email);
        Assert.StartsWith("NV", result.EmployeeCode);
        Assert.True(result.IsProfileComplete);
        Assert.False(result.IsLinkedToAccount);

        var saved = await fixture.Context.EmployeeProfiles
            .SingleAsync();

        Assert.Equal(result.Id, saved.Id);
        Assert.Equal("Kinh doanh", saved.Department);
    }

    // =========================================
    // TEST 2: HO TEN KHONG HOP LE
    // =========================================

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task CreateAsync_InvalidName_ShouldFail(
        string fullName)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest();
        request.FullName = fullName;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(fixture.Context.EmployeeProfiles);
    }

    // =========================================
    // TEST 3: SO DIEN THOAI SAI DINH DANG
    // =========================================

    [Theory]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("09123")]
    [InlineData("abcdefghij")]
    public async Task CreateAsync_InvalidPhone_ShouldFail(
        string phone)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest();
        request.PhoneNumber = phone;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(fixture.Context.EmployeeProfiles);
    }

    // =========================================
    // TEST 4: EMAIL SAI DINH DANG
    // =========================================

    [Theory]
    [InlineData("email-sai")]
    [InlineData("abc@")]
    public async Task CreateAsync_InvalidEmail_ShouldFail(
        string email)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest(email);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));
    }

    // =========================================
    // TEST 5: NGAY SINH KHONG HOP LE
    // =========================================

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateAsync_BirthDateNotInPast_ShouldFail(
        int offset)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest();

        request.DateOfBirth = DateOnly
            .FromDateTime(DateTime.Today)
            .AddDays(offset);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));
    }

    // =========================================
    // TEST 6: THIEU CHUC VU HOAC PHONG BAN
    // =========================================

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateAsync_MissingRequiredField_ShouldFail(
        bool missingPosition)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var request = ValidRequest();

        if (missingPosition)
            request.Position = " ";
        else
            request.Department = "";

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));
    }

    // =========================================
    // TEST 7: KHONG CHO PHEP TRUNG EMAIL
    // =========================================

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        await service.CreateAsync(ValidRequest());

        var duplicate = ValidRequest("AN@EXAMPLE.COM");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(duplicate));

        Assert.Equal(
            1,
            await fixture.Context.EmployeeProfiles.CountAsync());
    }

    // =========================================
    // TEST 8: LIEN KET VOI TAI KHOAN EMPLOYEE
    // =========================================

    [Fact]
    public async Task CreateAsync_ExistingEmployee_ShouldLinkAccount()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var account = NewUser("an@example.com");

        fixture.Context.Users.Add(account);
        await fixture.Context.SaveChangesAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var result = await service.CreateAsync(
            ValidRequest("an@example.com"));

        Assert.True(result.IsLinkedToAccount);
        Assert.Equal(account.Id, result.UserId);
        Assert.Equal("Nguyen Van An", result.FullName);

        var saved = await fixture.Context.EmployeeProfiles
            .Include(p => p.User)
            .SingleAsync();

        Assert.Equal(account.Id, saved.UserId);
        Assert.NotNull(saved.User);
        Assert.Equal("Nguyen Van An", saved.User.FullName);
        Assert.Equal("0912345678", saved.User.PhoneNumber);
    }

    // =========================================
    // TEST 9: KHONG LIEN KET TAI KHOAN MANAGER
    // =========================================

    [Fact]
    public async Task CreateAsync_ManagerEmail_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        fixture.Context.Users.Add(
            NewUser("an@example.com", UserRole.Manager));

        await fixture.Context.SaveChangesAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(ValidRequest()));

        Assert.Empty(fixture.Context.EmployeeProfiles);
    }

    // =========================================
    // TEST 10: LAY DANH SACH HO SO
    // =========================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnProfilesAndLegacyUsers()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        // Ho so chua lien ket tai khoan
        await service.CreateAsync(ValidRequest());

        // Employee cu chua co ho so
        var employee = NewUser("cu@example.com");

        // Manager khong duoc dua vao danh sach
        var manager = NewUser(
            "manager@example.com",
            UserRole.Manager);

        fixture.Context.Users.AddRange(employee, manager);
        await fixture.Context.SaveChangesAsync();

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            x => x.Email == "an@example.com"
                 && x.IsProfileComplete);

        Assert.Contains(
            result,
            x => x.Email == "cu@example.com"
                 && !x.IsProfileComplete);

        Assert.DoesNotContain(
            result,
            x => x.Email == "manager@example.com");
    }

    // =========================================
    // TEST 11: XEM CHI TIET HO SO
    // =========================================

    [Fact]
    public async Task GetByIdAsync_ExistingProfile_ReturnsProfile()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var created = await service.CreateAsync(
            ValidRequest());

        var result = await service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Nguyen Van An", result.FullName);
        Assert.Equal("Kinh doanh", result.Department);
    }

    // =========================================
    // TEST 12: KHONG TIM THAY HO SO
    // =========================================

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new EmployeeProfileService(
            fixture.Context);

        var result = await service.GetByIdAsync(
            Guid.NewGuid());

        Assert.Null(result);
    }
}
