
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data;

using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.ShiftAssignments;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Domain.Enums;
using HRShiftManagement.Infrastructure.Persistence.Context;

using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Services;

public class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly ApplicationDbContext _db;

    public ShiftAssignmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    // =====================================================
    // US 2.2 - 1. Lay danh sach nhan vien dang hoat dong
    // =====================================================
    public async Task<List<EmployeeOptionResponse>>
        GetEmployeesAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Employee
                     && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new EmployeeOptionResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email
            })
            .ToListAsync(ct);
    }

    public async Task<List<ScheduleCellResponse>>
        GetScheduleAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
    {
        if (from == default ||
            to == default ||
            to < from ||
            to.DayNumber - from.DayNumber > 62)
        {
            throw new ArgumentException(
                "Khoang ngay khong hop le (toi da 63 ngay).");
        }

        var shifts = await _db.Shifts
            .AsNoTracking()
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        var assignments = await _db.ShiftAssignments
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a =>
                a.WorkDate >= from &&
                a.WorkDate <= to)
            .ToListAsync(ct);

        var result = new List<ScheduleCellResponse>();

        for (
            var day = from;
            day <= to;
            day = day.AddDays(1))
        {
            foreach (var shift in shifts)
            {
                var assigned = assignments
                    .Where(a =>
                        a.WorkDate == day &&
                        a.ShiftId == shift.Id)
                    .ToList();

                result.Add(new ScheduleCellResponse
                {
                    WorkDate = day,
                    ShiftId = shift.Id,
                    ShiftName = shift.Name,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    MinEmployees = shift.MinEmployees,
                    MaxEmployees = shift.MaxEmployees,
                    AssignedCount = assigned.Count,

                    Employees = assigned
                        .Select(a => new EmployeeOptionResponse
                        {
                            Id = a.Employee.Id,
                            FullName = a.Employee.FullName,
                            Email = a.Employee.Email
                        })
                        .ToList()
                });
            }
        }

        return result;
    }

    // =====================================================
    // US 2.3 - 3. Lay lich lam viec cua mot nhan vien
    // Danh cho Employee
    // =====================================================
    public async Task<List<EmployeeScheduleResponse>>
        GetEmployeeScheduleAsync(
            Guid employeeId,
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
    {
        // Kiem tra ma nhan vien
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Nhan vien khong hop le.");
        }

        // Kiem tra khoang ngay, toi da 7 ngay
        if (from == default ||
            to == default ||
            to < from ||
            to.DayNumber - from.DayNumber > 6)
        {
            throw new ArgumentException(
                "Khoang ngay khong hop le, toi da 7 ngay.");
        }

        // Lay lich lam viec cua nhan vien
        // Du lieu lay tu ShiftAssignments va Shifts
        return await _db.ShiftAssignments
            .AsNoTracking()
            .Where(a =>
                a.EmployeeId == employeeId &&
                a.WorkDate >= from &&
                a.WorkDate <= to)
            .OrderBy(a => a.WorkDate)
            .ThenBy(a => a.Shift.StartTime)
            .Select(a => new EmployeeScheduleResponse
            {
                WorkDate = a.WorkDate,
                ShiftName = a.Shift.Name,
                StartTime = a.Shift.StartTime,
                EndTime = a.Shift.EndTime
            })
            .ToListAsync(ct);
    }

    // =====================================================
    // US 2.2 - 4. Phan mot hoac nhieu nhan vien vao ca
    // =====================================================
    public async Task AssignAsync(
        AssignEmployeesRequest request,
        CancellationToken ct = default)
    {
        // Kiem tra du lieu dau vao
        if (request.ShiftId == Guid.Empty ||
            request.WorkDate == default ||
            request.EmployeeIds == null ||
            request.EmployeeIds.Count == 0)
        {
            throw new ArgumentException(
                "Vui long chon ngay, ca va nhan vien.");
        }

        var ids = request.EmployeeIds
            .Distinct()
            .ToList();

        if (ids.Count != request.EmployeeIds.Count ||
            ids.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "Danh sach nhan vien khong hop le.");
        }

        // Bat dau Transaction
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

        // Tim ca lam viec
        var shift = await _db.Shifts
            .FirstOrDefaultAsync(
                s => s.Id == request.ShiftId, ct);

        if (shift == null)
        {
            throw new KeyNotFoundException(
                "Khong tim thay ca lam viec.");
        }

        // Kiem tra nhan vien hop le
        var validCount = await _db.Users.CountAsync(
            u => ids.Contains(u.Id)
              && u.Role == UserRole.Employee
              && u.Status == UserStatus.Active,
            ct);

        if (validCount != ids.Count)
        {
            throw new ArgumentException(
                "Co nhan vien khong ton tai, khong hoat dong " +
                "hoac khong thuoc vai tro Employee.");
        }

        // Kiem tra so luong nhan vien toi da
        var currentCount = await _db.ShiftAssignments
            .CountAsync(a =>
                a.ShiftId == request.ShiftId &&
                a.WorkDate == request.WorkDate,
                ct);

        if (currentCount + ids.Count > shift.MaxEmployees)
        {
            throw new ShiftConflictException(
                $"Ca chi cho phep toi da {shift.MaxEmployees} " +
                $"nhan vien. Hien da co {currentCount} nguoi.");
        }

        // Kiem tra lich trung trong ngay va ngay lien ke
        // Ho tro ca lam viec qua dem
        var from = request.WorkDate.AddDays(-1);
        var to = request.WorkDate.AddDays(1);

        var existing = await _db.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a =>
                ids.Contains(a.EmployeeId) &&
                a.WorkDate >= from &&
                a.WorkDate <= to)
            .ToListAsync(ct);

        foreach (var employeeId in ids)
        {
            foreach (
                var assignment in existing
                    .Where(a => a.EmployeeId == employeeId))
            {
                // Nhan vien da duoc phan vao ca nay
                if (assignment.ShiftId == shift.Id &&
                    assignment.WorkDate == request.WorkDate)
                {
                    throw new ShiftConflictException(
                        "Nhan vien da duoc phan vao ca nay.");
                }

                // Kiem tra trung thoi gian lam viec
                if (IsOverlapping(
                    shift,
                    request.WorkDate,
                    assignment.Shift,
                    assignment.WorkDate))
                {
                    throw new ShiftConflictException(
                        "Nhan vien da co ca lam viec trung gio.");
                }
            }
        }

        // Luu cac nhan vien vao ca
        foreach (var employeeId in ids)
        {
            _db.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                ShiftId = shift.Id,
                EmployeeId = employeeId,
                WorkDate = request.WorkDate,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        // Xac nhan Transaction
        await transaction.CommitAsync(ct);
    }

    // =====================================================
    // US 2.2 - 5. Kiem tra hai ca co trung thoi gian
    // =====================================================
    private static bool IsOverlapping(
        Shift first,
        DateOnly firstDate,
        Shift second,
        DateOnly secondDate)
    {
        var start1 = firstDate.ToDateTime(first.StartTime);
        var end1 = firstDate.ToDateTime(first.EndTime);

        var start2 = secondDate.ToDateTime(second.StartTime);
        var end2 = secondDate.ToDateTime(second.EndTime);

        // Xu ly ca qua dem
        if (end1 <= start1)
        {
            end1 = end1.AddDays(1);
        }

        if (end2 <= start2)
        {
            end2 = end2.AddDays(1);
        }

        // Hai khoang thoi gian giao nhau
        return start1 < end2 && start2 < end1;
    }

    // =====================================================
    // US 2.2 - 6. Xoa mot nhan vien khoi ca lam viec
    // =====================================================
    public async Task<bool> RemoveAsync(
        Guid shiftId,
        DateOnly workDate,
        Guid employeeId,
        CancellationToken ct = default)
    {
        // Tim phan cong cua nhan vien
        var assignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(a =>
                a.ShiftId == shiftId &&
                a.WorkDate == workDate &&
                a.EmployeeId == employeeId,
                ct);

        if (assignment == null)
        {
            return false;
        }

        // Chi xoa lich phan cong
        // Khong xoa nhan vien hoac ca lam viec
        _db.ShiftAssignments.Remove(assignment);

        await _db.SaveChangesAsync(ct);

        return true;
    }
}
