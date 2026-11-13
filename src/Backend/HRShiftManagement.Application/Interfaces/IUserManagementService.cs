using HRShiftManagement.Application.Common;
using HRShiftManagement.Application.DTOs.Users;

namespace HRShiftManagement.Application.Interfaces;

public interface IUserManagementService
{
    Task<Result<List<UserListItemDto>>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateUserRoleAsync(
        Guid userId,
        string role,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);
    Task<Result<UserListItemDto>> CreateUserAsync(
    CreateUserRequest request,
    CancellationToken cancellationToken = default);
    Task<Result<UserListItemDto>> UpdateUserAsync(
    Guid userId,
    UpdateUserRequest request,
    CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserStatusAsync(
    Guid userId,
    string status,
    Guid performedByUserId,
    CancellationToken cancellationToken = default);
}