using HRShiftManagement.Application.DTOs.Employees;

namespace HRShiftManagement.Application.Interfaces;

public interface IEmployeeProfileService
{
    // Lay danh sach nhan vien
    Task<List<EmployeeProfileResponse>> GetAllAsync(
        CancellationToken ct = default);

    // Xem chi tiet ho so
    Task<EmployeeProfileResponse?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    // Them ho so nhan vien
    Task<EmployeeProfileResponse> CreateAsync(
        EmployeeProfileRequest request,
        CancellationToken ct = default);

    // Cap nhat ho so nhan vien
    Task<EmployeeProfileResponse> UpdateAsync(
        Guid id,
        EmployeeProfileRequest request,
        CancellationToken ct = default);
}
