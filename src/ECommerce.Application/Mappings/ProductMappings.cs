using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

public static class ProductMappings
{
    public static ProductSummaryResponse ToSummary(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        CategoryName = product.Category?.Name ?? string.Empty,
        StockQuantity = product.Inventory?.Quantity ?? 0,
        IsActive = product.IsActive
    };

    public static ProductResponse ToResponse(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Sku = product.Sku,
        ImageUrl = product.ImageUrl,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        IsActive = product.IsActive,
        StockQuantity = product.Inventory?.Quantity ?? 0,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
