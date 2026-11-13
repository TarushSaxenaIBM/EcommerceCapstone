namespace ECommerce.Application.Common;

public record ProductSnapshot(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    int StockQuantity);
