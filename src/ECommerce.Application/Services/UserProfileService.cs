using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ILogger<UserProfileService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserResponse> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new Exceptions.NotFoundException("User not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Profile updated for user {UserId}.", user.Id);

        return user.ToResponse();
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _users.GetAllAsync(ct);
        return users.Select(u => u.ToResponse()).ToList();
    }

    public async Task<UserResponse> UpdateRoleAsync(
        int userId,
        UpdateUserRoleRequest request,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new Exceptions.NotFoundException("User not found.");

        user.Role = request.Role!.Value;

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Role for user {UserId} changed to {Role}.", user.Id, user.Role);

        return user.ToResponse();
    }
}
