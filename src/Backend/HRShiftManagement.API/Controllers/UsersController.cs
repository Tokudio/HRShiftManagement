using System.Security.Claims;
using HRShiftManagement.Application.DTOs.Users;
using HRShiftManagement.Application.Interfaces;
using HRShiftManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HRShiftManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetUsersAsync(
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage
            });
        }

        return Ok(result.Data);
    }

    // PUT: api/Users/{id}/role
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(
    Guid id,
    [FromBody] UpdateUserRoleRequest request,
    CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var performedByUserId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        var result = await _userManagementService.UpdateUserRoleAsync(
            id,
            request.Role,
            performedByUserId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage
            });
        }

        return Ok(new
        {
            message = "User role updated successfully."
        });
    }
}