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

    /// <summary>
    /// Creates a new user account. Only Admin can perform this operation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.CreateUserAsync(
            request,
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains(
                "already exists",
                StringComparison.OrdinalIgnoreCase) == true)
            {
                return Conflict(new
                {
                    message = result.ErrorMessage
                });
            }

            return BadRequest(new
            {
                message = result.ErrorMessage
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Data);
    }

    /// <summary>
    /// Updates user profile information. Only Admin can perform this operation.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.UpdateUserAsync(
            id,
            request,
            cancellationToken);

        if (!result.IsSuccess)
        {
            string error = result.ErrorMessage ?? "Unable to update user.";

            if (error.Contains(
                "not found",
                StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }

            if (error.Contains(
                "already exists",
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = error });
            }

            return BadRequest(new { message = error });
        }

        return Ok(result.Data);
    }


    /// <summary>
    /// Locks or activates a user account. Only Admin can perform this operation.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Get the ID of the currently logged-in Admin
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var performedByUserId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        // 2. Call the service
        var result = await _userManagementService.UpdateUserStatusAsync(
            id,
            request.Status,
            performedByUserId,
            cancellationToken);

        // 3. Handle errors
        if (!result.IsSuccess)
        {
            string error = result.ErrorMessage ?? "Unable to update user status.";

            if (error.Contains(
                "not found",
                StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = error });
            }

            if (error.Contains(
                "last active administrator",
                StringComparison.OrdinalIgnoreCase) ||
                error.Contains(
                "cannot lock your own account",
                StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = error });
            }

            return BadRequest(new { message = error });
        }

        // 4. Return success
        return Ok(new
        {
            message = "User status updated successfully."
        });
    }

}