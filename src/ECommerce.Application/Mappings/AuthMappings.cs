using ECommerce.Application.DTOs.Auth;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

public static class AuthMappings
{
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}
