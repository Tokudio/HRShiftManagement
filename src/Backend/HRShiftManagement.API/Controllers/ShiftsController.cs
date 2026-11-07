
using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Shifts;
using HRShiftManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize(Roles = "Manager,Admin")]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _service;

    public ShiftsController(IShiftService service)
    {
        _service = service;
    }

    // 1. Lay danh sach ca lam viec
    [HttpGet]
    public async Task<IActionResult> List(
        CancellationToken ct)
    {
        var shifts = await _service.ListAsync(ct);
        return Ok(shifts);
    }

    // 2. Lay thong tin mot ca
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id, CancellationToken ct)
    {
        var shift = await _service.GetAsync(id, ct);

        if (shift == null)
            return NotFound(new
            {
                message = "Khong tim thay ca lam viec."
            });

        return Ok(shift);
    }

    // 3. Tao ca lam viec moi
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ShiftRequest request,
        CancellationToken ct)
    {
        try
        {
            var shift = await _service.CreateAsync(
                request, ct);

            return CreatedAtAction(
                nameof(Get),
                new { id = shift.Id },
                shift);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ShiftConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // 4. Chinh sua ca lam viec
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ShiftRequest request,
        CancellationToken ct)
    {
        try
        {
            var shift = await _service.UpdateAsync(
                id, request, ct);

            if (shift == null)
                return NotFound(new
                {
                    message = "Khong tim thay ca lam viec."
                });

            return Ok(shift);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ShiftConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // 5. Xoa ca lam viec
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, CancellationToken ct)
    {
        try
        {
            bool deleted = await _service.DeleteAsync(
                id, ct);

            if (!deleted)
                return NotFound(new
                {
                    message = "Khong tim thay ca lam viec."
                });

            return NoContent();
        }
        catch (ShiftConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}
