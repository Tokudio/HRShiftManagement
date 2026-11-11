using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using HRShiftManagement.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/my-schedule")]
[Authorize(Roles = "Employee")]
public class MyScheduleController : ControllerBase
{
    private readonly IShiftAssignmentService _service;

    public MyScheduleController(
        IShiftAssignmentService service)
    {
        _service = service;
    }

    // US 2.3 - Nhan vien xem lich cua minh
    [HttpGet]
    public async Task<IActionResult> GetMySchedule(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        // Lay ID cua tai khoan dang nhap tu JWT
        var userIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var employeeId))
        {
            return Unauthorized(new
            {
                message = "Khong xac dinh duoc nhan vien."
            });
        }

        try
        {
            var result =
                await _service.GetEmployeeScheduleAsync(
                    employeeId, from, to, ct);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
