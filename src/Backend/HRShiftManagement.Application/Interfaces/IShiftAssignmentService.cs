using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HRShiftManagement.Application.DTOs.ShiftAssignments;

namespace HRShiftManagement.Application.Interfaces;

public interface IShiftAssignmentService
{
    // Lay danh sach nhan vien de chon
    Task<List<EmployeeOptionResponse>> GetEmployeesAsync(
        CancellationToken ct = default);

    // Lay lich lam viec trong khoang ngay
    Task<List<ScheduleCellResponse>> GetScheduleAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    // Phan mot hoac nhieu nhan vien vao ca
    Task AssignAsync(
        AssignEmployeesRequest request,
        CancellationToken ct = default);


    Task<bool> RemoveAsync(
        Guid shiftId,
        DateOnly workDate,
        Guid employeeId,
        CancellationToken ct = default);

}
