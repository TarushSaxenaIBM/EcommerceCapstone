using ECommerce.Application.DTOs.Categories;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive
    };
}
