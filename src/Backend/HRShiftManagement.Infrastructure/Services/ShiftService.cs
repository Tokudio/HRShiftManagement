using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Shifts;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly ApplicationDbContext _db;

    public ShiftService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Lay danh sach ca
    public Task<List<ShiftResponse>> ListAsync(
        CancellationToken ct = default)
    {
        return _db.Shifts.AsNoTracking()
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftResponse
            {
                Id = s.Id,
                Name = s.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                MinEmployees = s.MinEmployees,
                MaxEmployees = s.MaxEmployees,
                CanDelete = !s.Assignments.Any()
            })
            .ToListAsync(ct);
    }

    // Lay chi tiet mot ca
    public Task<ShiftResponse?> GetAsync(
        Guid id, CancellationToken ct = default)
    {
        return _db.Shifts.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShiftResponse
            {
                Id = s.Id,
                Name = s.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                MinEmployees = s.MinEmployees,
                MaxEmployees = s.MaxEmployees,
                CanDelete = !s.Assignments.Any()
            })
            .FirstOrDefaultAsync(ct);
    }

    // Them ca moi
    public async Task<ShiftResponse> CreateAsync(
        ShiftRequest request,
        CancellationToken ct = default)
    {
        var name = Validate(request);
        await CheckNameAsync(name, null, ct);

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            StartTime = request.StartTime!.Value,
            EndTime = request.EndTime!.Value,
            MinEmployees = request.MinEmployees,
            MaxEmployees = request.MaxEmployees,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shifts.Add(shift);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ShiftConflictException(
                "Khong the tao ca. Ten ca co the da ton tai.");
        }

        return ToResponse(shift, true);
    }

    // Chinh sua ca
    public async Task<ShiftResponse?> UpdateAsync(
        Guid id, ShiftRequest request,
        CancellationToken ct = default)
    {
        var name = Validate(request);

        var shift = await _db.Shifts
            .FindAsync(new object[] { id }, ct);

        if (shift == null)
            return null;

        await CheckNameAsync(name, id, ct);

        shift.Name = name;
        shift.StartTime = request.StartTime!.Value;
        shift.EndTime = request.EndTime!.Value;
        shift.MinEmployees = request.MinEmployees;
        shift.MaxEmployees = request.MaxEmployees;
        shift.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ShiftConflictException(
                "Khong the cap nhat ca lam viec.");
        }

        bool canDelete = !await _db.ShiftAssignments
            .AnyAsync(a => a.ShiftId == id, ct);

        return ToResponse(shift, canDelete);
    }

    // Xoa ca neu khong co lich phan cong
    public async Task<bool> DeleteAsync(
        Guid id, CancellationToken ct = default)
    {
        var shift = await _db.Shifts
            .FindAsync(new object[] { id }, ct);

        if (shift == null)
            return false;

        bool hasAssignments = await _db.ShiftAssignments
            .AnyAsync(a => a.ShiftId == id, ct);

        if (hasAssignments)
        {
            throw new ShiftConflictException(
                "Khong the xoa ca da co lich phan cong.");
        }

        _db.Shifts.Remove(shift);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ShiftConflictException(
                "Khong the xoa vi ca dang duoc su dung.");
        }

        return true;
    }


    // Kiem tra thong tin ca lam viec
    private static string Validate(ShiftRequest request)
    {
        var name = (request.Name ?? "").Trim();

        if (name.Length < 2 || name.Length > 100)
        {
            throw new ArgumentException(
                "Ten ca phai co tu 2 den 100 ky tu.");
        }

        if (request.StartTime == null ||
            request.EndTime == null)
        {
            throw new ArgumentException(
                "Vui long nhap thoi gian lam viec.");
        }

        var start = request.StartTime.Value.ToTimeSpan();
        var end = request.EndTime.Value.ToTimeSpan();

        // Khong cho phep gio bat dau trung gio ket thuc
        if (start == end)
        {
            throw new ArgumentException(
                "Gio bat dau khong duoc trung gio ket thuc.");
        }

        // Xac dinh ca qua dem
        bool isOvernight = end < start;

        if (isOvernight && !request.ConfirmOvernight)
        {
            throw new ArgumentException(
                "Gio ket thuc nho hon gio bat dau. " +
                "Vui long xac nhan ca lam viec qua dem.");
        }

        // Tinh thoi luong ca
        TimeSpan duration = end - start;

        // Neu ca qua dem thi cong them 1 ngay
        if (isOvernight)
        {
            duration += TimeSpan.FromDays(1);
        }

        // Gioi han tu 30 phut den 12 gio
        if (duration < TimeSpan.FromMinutes(30) ||
            duration > TimeSpan.FromHours(12))
        {
            throw new ArgumentException(
                "Thoi luong ca phai tu 30 phut den 12 gio.");
        }

        if (request.MinEmployees < 1 ||
            request.MaxEmployees < request.MinEmployees ||
            request.MaxEmployees > 10000)
        {
            throw new ArgumentException(
                "So luong nhan vien khong hop le.");
        }

        return name;
    }


    // Kiem tra ten ca trung
    private async Task CheckNameAsync(
        string name, Guid? excludeId,
        CancellationToken ct)
    {
        bool exists = await _db.Shifts.AnyAsync(
            s => s.Name.ToUpper() == name.ToUpper()
              && (!excludeId.HasValue ||
                  s.Id != excludeId.Value), ct);

        if (exists)
            throw new ShiftConflictException(
                "Ten ca da ton tai.");
    }

    // Chuyen Entity thanh DTO
    private static ShiftResponse ToResponse(
        Shift shift, bool canDelete)
    {
        return new ShiftResponse
        {
            Id = shift.Id,
            Name = shift.Name,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            MinEmployees = shift.MinEmployees,
            MaxEmployees = shift.MaxEmployees,
            CanDelete = canDelete
        };
    }
}
