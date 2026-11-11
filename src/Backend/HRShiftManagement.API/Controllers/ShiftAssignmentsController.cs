using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.ShiftAssignments;
using HRShiftManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/shift-assignments")]
[Authorize(Roles = "Manager,Admin")]
public class ShiftAssignmentsController : ControllerBase
{
    private readonly IShiftAssignmentService _service;

    public ShiftAssignmentsController(
        IShiftAssignmentService service)
    {
        _service = service;
    }

    // 1. Lay danh sach nhan vien
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees(
        CancellationToken ct)
    {
        var employees = await _service.GetEmployeesAsync(ct);

        return Ok(employees);
    }

    // 2. Lay lich lam viec theo khoang ngay
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        try
        {
            var schedule = await _service.GetScheduleAsync(
                from, to, ct);

            return Ok(schedule);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // 3. Phan nhan vien vao ca
    [HttpPost]
    public async Task<IActionResult> AssignEmployees(
        [FromBody] AssignEmployeesRequest request,
        CancellationToken ct)
    {
        try
        {
            await _service.AssignAsync(request, ct);

            return Ok(new
            {
                message = "Phân nhân viên vào ca thành công."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
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

    // Huy phan cong nhan vien khoi ca
    [HttpDelete("{shiftId:guid}/employees/{employeeId:guid}")]
    public async Task<IActionResult> RemoveEmployee(
        Guid shiftId,
        Guid employeeId,
        [FromQuery] DateOnly workDate,
        CancellationToken ct)
    {
        if (workDate == default)
        {
            return BadRequest(new
            {
                message = "Ngay lam viec khong hop le."
            });
        }

        bool removed = await _service.RemoveAsync(
            shiftId, workDate, employeeId, ct);

        if (!removed)
        {
            return NotFound(new
            {
                message = "Khong tim thay lich phan cong."
            });
        }

        return NoContent();
    }

}
