using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class EmployeeScheduleServiceTests
{
    // Ngay su dung de kiem thu
    private static readonly DateOnly WorkDate =
        new DateOnly(2026, 11, 20);

    // ============================================
    // TAO NHAN VIEN MAU
    // ============================================

    private static User NewEmployee(string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = email,
            Email = email,
            PasswordHash = "test-hash",
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================
    // TAO CA LAM VIEC MAU
    // ============================================

    private static Shift NewShift(
        string name,
        int startHour,
        int endHour)
    {
        return new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0),
            MinEmployees = 1,
            MaxEmployees = 5,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================
    // TAO DU LIEU PHAN CONG MAU
    // ============================================

    private static ShiftAssignment NewAssignment(
        User employee,
        Shift shift,
        DateOnly date)
    {
        return new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ShiftId = shift.Id,
            WorkDate = date,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================
    // TEST 1: CHI HIEN THI LICH CUA NHAN VIEN
    // ============================================

    [Fact]
    public async Task GetEmployeeScheduleAsync_OnlyOwnShifts()
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        // Tao 2 nhan vien
        var employeeA = NewEmployee("a@test.local");
        var employeeB = NewEmployee("b@test.local");

        // Tao 2 ca
        var morning = NewShift("Ca sang", 8, 12);
        var afternoon = NewShift("Ca chieu", 13, 17);

        fixture.Context.Users.AddRange(
            employeeA,
            employeeB
        );

        fixture.Context.Shifts.AddRange(
            morning,
            afternoon
        );

        // Moi nhan vien co mot ca rieng
        fixture.Context.ShiftAssignments.AddRange(
            NewAssignment(employeeA, morning, WorkDate),
            NewAssignment(employeeB, afternoon, WorkDate)
        );

        await fixture.Context.SaveChangesAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        // Nhan vien A xem lich cua minh
        var result =
            await service.GetEmployeeScheduleAsync(
                employeeA.Id,
                WorkDate,
                WorkDate
            );

        // Chi thay ca sang
        var item = Assert.Single(result);

        Assert.Equal("Ca sang", item.ShiftName);

        Assert.Equal(WorkDate, item.WorkDate);

        Assert.Equal(
            new TimeOnly(8, 0),
            item.StartTime
        );
    }

    // ============================================
    // TEST 2: LICH SAP XEP THEO NGAY VA GIO
    // ============================================

    [Fact]
    public async Task GetEmployeeScheduleAsync_SortedByDateAndTime()
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        var employee = NewEmployee("a@test.local");

        var morning = NewShift("Ca sang", 8, 12);
        var afternoon = NewShift("Ca chieu", 13, 17);

        fixture.Context.Users.Add(employee);

        fixture.Context.Shifts.AddRange(
            morning,
            afternoon
        );

        // Co tinh them du lieu khong theo thu tu
        fixture.Context.ShiftAssignments.AddRange(
            NewAssignment(employee, afternoon, WorkDate),
            NewAssignment(employee, morning, WorkDate),
            NewAssignment(
                employee,
                morning,
                WorkDate.AddDays(1)
            )
        );

        await fixture.Context.SaveChangesAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        var result =
            await service.GetEmployeeScheduleAsync(
                employee.Id,
                WorkDate,
                WorkDate.AddDays(1)
            );

        Assert.Equal(3, result.Count);

        // Ca sang hien truoc ca chieu
        Assert.Equal("Ca sang", result[0].ShiftName);

        Assert.Equal("Ca chieu", result[1].ShiftName);

        // Ca ngay hom sau hien cuoi
        Assert.Equal(
            WorkDate.AddDays(1),
            result[2].WorkDate
        );
    }

    // ============================================
    // TEST 3: CHUA DUOC PHAN CA THI LICH RONG
    // ============================================

    [Fact]
    public async Task GetEmployeeScheduleAsync_NoShifts_ReturnsEmpty()
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        var employee = NewEmployee("a@test.local");

        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        var result =
            await service.GetEmployeeScheduleAsync(
                employee.Id,
                WorkDate,
                WorkDate
            );

        Assert.Empty(result);
    }

    // ============================================
    // TEST 4: ID NHAN VIEN KHONG HOP LE
    // ============================================

    [Fact]
    public async Task GetEmployeeScheduleAsync_EmptyEmployeeId_Throws()
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetEmployeeScheduleAsync(
                Guid.Empty,
                WorkDate,
                WorkDate
            )
        );
    }

    // ============================================
    // TEST 5 & 6: KHOANG NGAY KHONG HOP LE
    // ============================================

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 7)]
    public async Task GetEmployeeScheduleAsync_InvalidDateRange_Throws(
        int fromOffset,
        int toOffset)
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetEmployeeScheduleAsync(
                Guid.NewGuid(),
                WorkDate.AddDays(fromOffset),
                WorkDate.AddDays(toOffset)
            )
        );
    }

    // ============================================
    // TEST 7: XOA PHAN CA THI LICH DUOC CAP NHAT
    // ============================================

    [Fact]
    public async Task GetEmployeeScheduleAsync_AfterRemoval_ReturnsEmpty()
    {
        await using var fixture =
            new TestDatabaseFixture();

        await fixture.InitializeAsync();

        var employee = NewEmployee("a@test.local");

        var shift = NewShift("Ca sang", 8, 12);

        fixture.Context.Users.Add(employee);

        fixture.Context.Shifts.Add(shift);

        fixture.Context.ShiftAssignments.Add(
            NewAssignment(employee, shift, WorkDate)
        );

        await fixture.Context.SaveChangesAsync();

        var service = new ShiftAssignmentService(
            fixture.Context
        );

        // Lich truoc khi xoa
        var before =
            await service.GetEmployeeScheduleAsync(
                employee.Id,
                WorkDate,
                WorkDate
            );

        // Manager huy phan cong
        var removed = await service.RemoveAsync(
            shift.Id,
            WorkDate,
            employee.Id
        );

        // Lich sau khi xoa
        var after =
            await service.GetEmployeeScheduleAsync(
                employee.Id,
                WorkDate,
                WorkDate
            );

        Assert.Single(before);

        Assert.True(removed);

        Assert.Empty(after);
    }
}
