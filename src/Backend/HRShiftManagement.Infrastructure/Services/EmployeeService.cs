using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRShiftManagement.Application.DTOs.Employee;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Entities;
using HRShiftManagement.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HRShiftManagement.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;

    public EmployeeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(
        CancellationToken ct)
    {
        return await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.EmployeeCode)
            .Select(e => ToResponse(e))
            .ToListAsync(ct);
    }

    public async Task<EmployeeResponse?> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return employee == null ? null : ToResponse(employee);
    }

    public async Task<EmployeeResponse> CreateAsync(
        EmployeeRequest request, CancellationToken ct)
    {
        string code = request.EmployeeCode.Trim();

        bool codeExists = await _db.Employees
            .AnyAsync(e => e.EmployeeCode == code, ct);

        if (codeExists)
            throw new ArgumentException("Mã nhân viên đã tồn tại.");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = code,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email)
                ? null : request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            DateOfBirth = request.DateOfBirth!.Value,
            Position = request.Position.Trim(),
            Department = request.Department.Trim(),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);

        return ToResponse(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(
        Guid id, EmployeeRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee == null)
            return null;

        string code = request.EmployeeCode.Trim();

        bool codeExists = await _db.Employees.AnyAsync(
            e => e.EmployeeCode == code && e.Id != id, ct);

        if (codeExists)
            throw new ArgumentException("Mã nhân viên đã được sử dụng.");

        employee.EmployeeCode = code;
        employee.FullName = request.FullName.Trim();

        employee.Email = string.IsNullOrWhiteSpace(request.Email)
            ? null : request.Email.Trim();

        employee.PhoneNumber = request.PhoneNumber.Trim();
        employee.DateOfBirth = request.DateOfBirth!.Value;
        employee.Position = request.Position.Trim();
        employee.Department = request.Department.Trim();
        employee.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToResponse(employee);
    }

    private static EmployeeResponse ToResponse(Employee e)
    {
        return new EmployeeResponse
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FullName = e.FullName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            DateOfBirth = e.DateOfBirth,
            Position = e.Position,
            Department = e.Department,
            Status = e.Status
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee == null)
            return false;

        _db.Employees.Remove(employee);

        await _db.SaveChangesAsync(ct);

        return true;
    }
}