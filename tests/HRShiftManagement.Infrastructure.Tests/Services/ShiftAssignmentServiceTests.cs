using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.ShiftAssignments;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Services;
using HRShiftManagement.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRShiftManagement.Infrastructure.Tests.Services;

public class ShiftAssignmentServiceTests
{
    // Ngay lam viec dung trong kiem thu
    private static readonly DateOnly WorkDate =
        new(2026, 11, 20);

    // ============================================
    // HAM TAO NHAN VIEN MAU
    // ============================================

    private static User NewEmployee(
        string email,
        UserRole role = UserRole.Employee,
        UserStatus status = UserStatus.Active)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nhan vien " + email,
            Email = email,
            PasswordHash = "test-hash",
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================
    // HAM TAO CA LAM VIEC MAU
    // ============================================

    private static Shift NewShift(
        string name,
        int startHour,
        int endHour,
        int max = 3)
    {
        return new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0),
            MinEmployees = 1,
            MaxEmployees = max,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================
    // HAM TAO YEU CAU PHAN CA
    // ============================================

    private static AssignEmployeesRequest Request(
        Shift shift,
        DateOnly date,
        params User[] users)
    {
        return new AssignEmployeesRequest
        {
            ShiftId = shift.Id,
            WorkDate = date,
            EmployeeIds = users
                .Select(u => u.Id)
                .ToList()
        };
    }

    // ============================================
    // TEST 1: CHI LAY NHAN VIEN DANG HOAT DONG
    // ============================================

    [Fact]
    public async Task GetEmployeesAsync_ReturnsOnlyActiveEmployees()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var active = NewEmployee("active@test.local");

        var inactive = NewEmployee(
            "inactive@test.local",
            status: UserStatus.Inactive);

        var manager = NewEmployee(
            "manager@test.local",
            role: UserRole.Manager);

        fixture.Context.Users.AddRange(
            active, inactive, manager);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        var result = await service.GetEmployeesAsync();

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    // ============================================
    // TEST 2: PHAN 2 NHAN VIEN THANH CONG
    // ============================================

    [Fact]
    public async Task AssignAsync_ValidEmployees_CreatesTwoAssignments()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);

        var a = NewEmployee("a@test.local");
        var b = NewEmployee("b@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.AddRange(a, b);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(shift, WorkDate, a, b));

        var assigned = await fixture.Context.ShiftAssignments
            .Where(x =>
                x.ShiftId == shift.Id &&
                x.WorkDate == WorkDate)
            .ToListAsync();

        Assert.Equal(2, assigned.Count);

        Assert.Contains(
            assigned,
            x => x.EmployeeId == a.Id);

        Assert.Contains(
            assigned,
            x => x.EmployeeId == b.Id);
    }

    // ============================================
    // TEST 3: KHONG CHO CHON TRUNG NHAN VIEN
    // ============================================

    [Fact]
    public async Task AssignAsync_DuplicateEmployeeIds_ThrowsArgumentException()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);
        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AssignAsync(
                Request(
                    shift,
                    WorkDate,
                    employee,
                    employee)));
    }

    // ============================================
    // TEST 4: KHONG PHAN CA CHO NHAN VIEN TAM NGHI
    // ============================================

    [Fact]
    public async Task AssignAsync_InactiveEmployee_IsRejected()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);

        var employee = NewEmployee(
            "inactive@test.local",
            status: UserStatus.Inactive);

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AssignAsync(
                Request(shift, WorkDate, employee)));

        Assert.Empty(
            await fixture.Context.ShiftAssignments.ToListAsync());
    }

    // ============================================
    // TEST 5: KHONG VUOT QUA SO NHAN VIEN TOI DA
    // ============================================

    [Fact]
    public async Task AssignAsync_ExceedsMaxEmployees_ThrowsConflict()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift(
            "Ca sang", 8, 12, max: 1);

        var a = NewEmployee("a@test.local");
        var b = NewEmployee("b@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.AddRange(a, b);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(shift, WorkDate, a));

        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.AssignAsync(
                Request(shift, WorkDate, b)));

        Assert.Single(
            await fixture.Context.ShiftAssignments.ToListAsync());
    }

    // ============================================
    // TEST 6: KHONG PHAN TRUNG CUNG MOT CA
    // ============================================

    [Fact]
    public async Task AssignAsync_SameEmployeeSameShiftTwice_ThrowsConflict()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);
        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(shift, WorkDate, employee));

        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.AssignAsync(
                Request(shift, WorkDate, employee)));

        Assert.Single(
            await fixture.Context.ShiftAssignments.ToListAsync());
    }

    // ============================================
    // TEST 7: PHAT HIEN 2 CA TRUNG GIO
    // ============================================

    [Fact]
    public async Task AssignAsync_OverlappingShiftsOnSameDay_ThrowsConflict()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var first = NewShift("Ca sang", 8, 12);
        var second = NewShift("Ca giao nhau", 11, 15);

        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.AddRange(first, second);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(first, WorkDate, employee));

        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.AssignAsync(
                Request(second, WorkDate, employee)));
    }

    // ============================================
    // TEST 8: HAI CA NOI TIEP NHAU KHONG TRUNG
    // ============================================

    [Fact]
    public async Task AssignAsync_ShiftsTouchButDoNotOverlap_AreAllowed()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var first = NewShift("Ca sang", 8, 12);
        var second = NewShift("Ca chieu", 12, 16);

        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.AddRange(first, second);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(first, WorkDate, employee));

        await service.AssignAsync(
            Request(second, WorkDate, employee));

        Assert.Equal(
            2,
            await fixture.Context.ShiftAssignments.CountAsync());
    }

    // ============================================
    // TEST 9: CA QUA DEM TRUNG CA SANG HOM SAU
    // ============================================

    [Fact]
    public async Task AssignAsync_OvernightOverlapsNextMorning_ThrowsConflict()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        // Ca dem 22:00 -> 06:00
        var night = NewShift("Ca dem", 22, 6);

        // Ca sang 05:00 -> 09:00
        var morning = NewShift("Ca som", 5, 9);

        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.AddRange(night, morning);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(night, WorkDate, employee));

        await Assert.ThrowsAsync<ShiftConflictException>(
            () => service.AssignAsync(
                Request(
                    morning,
                    WorkDate.AddDays(1),
                    employee)));
    }

    // ============================================
    // TEST 10: XOA MOT NHAN VIEN KHOI CA
    // ============================================

    [Fact]
    public async Task RemoveAsync_ExistingAssignment_RemovesOnlyMatchingEmployee()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);

        var a = NewEmployee("a@test.local");
        var b = NewEmployee("b@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.AddRange(a, b);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(shift, WorkDate, a, b));

        // Xoa nhan vien A
        var removed = await service.RemoveAsync(
            shift.Id,
            WorkDate,
            a.Id);

        Assert.True(removed);

        var remaining =
            await fixture.Context.ShiftAssignments.ToListAsync();

        Assert.Single(remaining);

        // Nhan vien B van con
        Assert.Equal(b.Id, remaining[0].EmployeeId);
    }

    // ============================================
    // TEST 11: KIEM TRA SO NHAN VIEN TRONG LICH
    // ============================================

    [Fact]
    public async Task GetScheduleAsync_AfterAssign_ReturnsActualAssignedCount()
    {
        await using var fixture = new TestDatabaseFixture();
        await fixture.InitializeAsync();

        var shift = NewShift("Ca sang", 8, 12);
        var employee = NewEmployee("a@test.local");

        fixture.Context.Shifts.Add(shift);
        fixture.Context.Users.Add(employee);

        await fixture.Context.SaveChangesAsync();

        var service =
            new ShiftAssignmentService(fixture.Context);

        await service.AssignAsync(
            Request(shift, WorkDate, employee));

        var result = await service.GetScheduleAsync(
            WorkDate,
            WorkDate);

        var cell = Assert.Single(result);

        Assert.Equal(1, cell.AssignedCount);
        Assert.Equal(shift.Id, cell.ShiftId);

        Assert.Equal(
            employee.Id,
            Assert.Single(cell.Employees).Id);
    }
}
