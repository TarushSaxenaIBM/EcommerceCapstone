using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Users;

public class UpdateProfileRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(50)]
    public string LastName { get; set; } = string.Empty;
}

public class UpdateUserRoleRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public UserRole? Role { get; set; }
}
