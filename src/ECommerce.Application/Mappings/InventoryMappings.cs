using ECommerce.Application.DTOs.Inventory;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

public static class InventoryMappings
{
    public static InventoryResponse ToResponse(this Inventory inventory) => new()
    {
        ProductId = inventory.ProductId,
        ProductName = inventory.Product?.Name ?? string.Empty,
        Quantity = inventory.Quantity,
        UpdatedAt = inventory.UpdatedAt
    };
}
