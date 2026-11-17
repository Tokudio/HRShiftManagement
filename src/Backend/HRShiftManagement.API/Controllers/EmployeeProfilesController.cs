using HRShiftManagement.Application.DTOs.Employees;
using HRShiftManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/employee-profiles")]
[Authorize(Roles = "Manager,Admin")]
public class EmployeeProfilesController : ControllerBase
{
    private readonly IEmployeeProfileService _employeeService;

    public EmployeeProfilesController(
        IEmployeeProfileService employeeService)
    {
        _employeeService = employeeService;
    }

    // ==================================================
    // US 1.5 - Lay danh sach tat ca nhan vien
    // GET: api/employee-profiles
    // ==================================================

    [HttpGet]
    [ProducesResponseType(
        typeof(List<EmployeeProfileResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmployeeProfileResponse>>>
        GetAll(CancellationToken ct)
    {
        var employees = await _employeeService.GetAllAsync(ct);

        return Ok(employees);
    }

    // ==================================================
    // US 1.5 - Xem chi tiet ho so nhan vien
    // GET: api/employee-profiles/{id}
    // ==================================================

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(EmployeeProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeProfileResponse>>
        GetById(Guid id, CancellationToken ct)
    {
        var employee = await _employeeService.GetByIdAsync(
            id, ct);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Khong tim thay ho so nhan vien."
            });
        }

        return Ok(employee);
    }

    // ==================================================
    // US 1.5 - Them ho so nhan vien moi
    // POST: api/employee-profiles
    // ==================================================

    [HttpPost]
    [ProducesResponseType(
        typeof(EmployeeProfileResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeProfileResponse>>
        Create(
            [FromBody] EmployeeProfileRequest request,
            CancellationToken ct)
    {
        try
        {
            var employee = await _employeeService.CreateAsync(
                request, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = employee.Id },
                employee
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ==================================================
    // US 1.6 - Cap nhat thong tin nhan vien
    // PUT: api/employee-profiles/{id}
    // ==================================================

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(EmployeeProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeProfileResponse>>
        Update(
            Guid id,
            [FromBody] EmployeeProfileRequest request,
            CancellationToken ct)
    {
        try
        {
            var employee = await _employeeService.UpdateAsync(
                id, request, ct);

            return Ok(new
            {
                message = "Cap nhat ho so nhan vien thanh cong.",
                data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}
