using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.UserService.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _profiles;

    public UsersController(IUserProfileService profiles)
    {
        _profiles = profiles;
    }

    /// <summary>Updates the signed-in customer's name.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.UpdateProfileAsync(User.GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>Lists every account. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken ct)
    {
        var result = await _profiles.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>Changes an account role. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/role")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateRole(
        int id,
        UpdateUserRoleRequest request,
        CancellationToken ct)
    {
        if (request.Role is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "A role value is required."
            });
        }

        var result = await _profiles.UpdateRoleAsync(id, request, ct);
        return Ok(result);
    }
}
