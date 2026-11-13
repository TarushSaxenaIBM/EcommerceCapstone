using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Interfaces.Services;

/// <summary>Profile and role management. Authentication lives in <see cref="IAuthService"/>.</summary>
public interface IUserProfileService
{
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UserResponse> UpdateRoleAsync(int userId, UpdateUserRoleRequest request, CancellationToken ct = default);
}
