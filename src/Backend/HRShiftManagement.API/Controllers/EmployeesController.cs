using HRShiftManagement.Application.DTOs.Employee;
using HRShiftManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "Admin,Manager")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id, CancellationToken ct)
    {
        var employee = await _service.GetByIdAsync(id, ct);

        if (employee == null)
            return NotFound(new { message = "Không tìm thấy nhân viên." });

        return Ok(employee);
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] EmployeeRequest request,
        CancellationToken ct)
    {
        try
        {
            var employee = await _service.CreateAsync(request, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = employee.Id },
                employee);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] EmployeeRequest request,
        CancellationToken ct)
    {
        try
        {
            var employee = await _service.UpdateAsync(id, request, ct);

            if (employee == null)
                return NotFound(new { message = "Không tìm thấy nhân viên." });

            return Ok(employee);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);

        if (!deleted)
            return NotFound(new { message = "Không tìm thấy nhân viên." });

        return Ok(new { message = "Xóa nhân viên thành công." });
    }
}