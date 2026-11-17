using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Shifts;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class ShiftServiceTests
{
    // Tao du lieu ca lam viec hop le de kiem thu
    private static ShiftRequest ValidRequest(
        string name = "Ca sang")
    {
        return new ShiftRequest
        {
            Name = name,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(12, 0),
            MinEmployees = 1,
            MaxEmployees = 3
        };
    }

    // ==================================================
    // TEST 1: TAO CA LAM VIEC THANH CONG
    // ==================================================

    [Fact]
    public async Task CreateAsync_ValidShift_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest();

        var result = await service.CreateAsync(request);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Ca sang", result.Name);
        Assert.Equal(new TimeOnly(8, 0), result.StartTime);
        Assert.Equal(new TimeOnly(12, 0), result.EndTime);
        Assert.True(result.CanDelete);

        Assert.Single(
            await fixture.Context.Shifts.ToListAsync()
        );
    }

    // ==================================================
    // TEST 2: KHONG CHO PHEP TEN CA RONG
    // ==================================================

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task CreateAsync_InvalidName_ShouldFail(
        string name)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest(name);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request)
        );

        Assert.Empty(
            await fixture.Context.Shifts.ToListAsync()
        );
    }

    // ==================================================
    // TEST 3: GIO BAT DAU TRUNG GIO KET THUC
    // ==================================================

    [Fact]
    public async Task CreateAsync_SameStartEnd_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest();

        request.EndTime = request.StartTime;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request)
        );
    }

    // ==================================================
    // TEST 4: SO LUONG NHAN VIEN KHONG HOP LE
    // ==================================================

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 2)]
    [InlineData(1, 10001)]
    public async Task CreateAsync_InvalidEmployeeLimits_ShouldFail(
        int min, int max)
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest();

        request.MinEmployees = min;
        request.MaxEmployees = max;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request)
        );
    }

    // ==================================================
    // TEST 5: CA QUA DEM CHUA XAC NHAN
    // ==================================================

    [Fact]
    public async Task CreateAsync_OvernightNotConfirmed_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest("Ca dem");

        request.StartTime = new TimeOnly(22, 0);
        request.EndTime = new TimeOnly(6, 0);
        request.ConfirmOvernight = false;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request)
        );
    }

    // ==================================================
    // TEST 6: CA QUA DEM DA XAC NHAN
    // ==================================================

    [Fact]
    public async Task CreateAsync_OvernightConfirmed_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var request = ValidRequest("Ca dem");

        request.StartTime = new TimeOnly(22, 0);
        request.EndTime = new TimeOnly(6, 0);
        request.ConfirmOvernight = true;

        var result = await service.CreateAsync(request);

        Assert.Equal(
            new TimeOnly(22, 0),
            result.StartTime
        );

        Assert.Equal(
            new TimeOnly(6, 0),
            result.EndTime
        );

        Assert.Single(
            await fixture.Context.Shifts.ToListAsync()
        );
    }

    // ==================================================
    // TEST 7: KHONG CHO PHEP TRUNG TEN CA
    // ==================================================

    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        await service.CreateAsync(
            ValidRequest("Ca sang")
        );

        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.CreateAsync(
                ValidRequest("ca SANG")
            )
        );

        Assert.Single(
            await fixture.Context.Shifts.ToListAsync()
        );
    }

    // ==================================================
    // TEST 8: CAP NHAT CA LAM VIEC
    // ==================================================

    [Fact]
    public async Task UpdateAsync_ValidShift_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var created = await service.CreateAsync(
            ValidRequest()
        );

        var request = ValidRequest("Ca chieu");

        request.StartTime = new TimeOnly(13, 0);
        request.EndTime = new TimeOnly(17, 0);
        request.MaxEmployees = 5;

        var result = await service.UpdateAsync(
            created.Id,
            request
        );

        Assert.NotNull(result);
        Assert.Equal("Ca chieu", result.Name);
        Assert.Equal(5, result.MaxEmployees);

        var saved = await fixture.Context.Shifts
            .FindAsync(created.Id);

        Assert.NotNull(saved);
        Assert.Equal(
            new TimeOnly(13, 0),
            saved.StartTime
        );
    }

    // ==================================================
    // TEST 9: XOA CA CHUA DUOC PHAN CONG
    // ==================================================

    [Fact]
    public async Task DeleteAsync_UnusedShift_ShouldSucceed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var shift = await service.CreateAsync(
            ValidRequest()
        );

        var result = await service.DeleteAsync(
            shift.Id
        );

        Assert.True(result);

        Assert.Empty(
            await fixture.Context.Shifts.ToListAsync()
        );
    }

    // ==================================================
    // TEST 10: KHONG XOA CA DA DUOC PHAN CONG
    // ==================================================

    [Fact]
    public async Task DeleteAsync_AssignedShift_ShouldFail()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var service = new ShiftService(fixture.Context);

        var shift = await service.CreateAsync(
            ValidRequest()
        );

        // Tao nhan vien mau
        var employee = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nhan vien A",
            Email = "employee-a@test.local",
            PasswordHash = "test-hash",
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        fixture.Context.Users.Add(employee);

        // Tao du lieu phan cong
        var assignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shift.Id,
            EmployeeId = employee.Id,
            WorkDate = new DateOnly(2026, 11, 20),
            CreatedAt = DateTime.UtcNow
        };

        fixture.Context.ShiftAssignments.Add(assignment);

        await fixture.Context.SaveChangesAsync();

        // Thu xoa ca da co phan cong
        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.DeleteAsync(shift.Id)
        );

        // Ca van phai ton tai
        Assert.True(
            await fixture.Context.Shifts.AnyAsync(
                s => s.Id == shift.Id
            )
        );
    }
}
