using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRShiftManagement.Application.DTOs.Employee;

namespace HRShiftManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<EmployeeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<EmployeeResponse> CreateAsync(
        EmployeeRequest request,
        CancellationToken cancellationToken);

    Task<EmployeeResponse?> UpdateAsync(
        Guid id,
        EmployeeRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}