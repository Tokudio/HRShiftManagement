using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HRShiftManagement.Application.DTOs.Shifts;

namespace HRShiftManagement.Application.Interfaces;

public interface IShiftService
{
    Task<List<ShiftResponse>> ListAsync(
        CancellationToken ct = default);

    Task<ShiftResponse?> GetAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ShiftResponse> CreateAsync(
        ShiftRequest request,
        CancellationToken ct = default);

    Task<ShiftResponse?> UpdateAsync(
        Guid id,
        ShiftRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}
