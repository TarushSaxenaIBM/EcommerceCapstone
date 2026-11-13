using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Inventory;

public class AdjustInventoryRequest
{
    [Range(-100000, 100000)]
    public int QuantityChange { get; set; }
}

public class InventoryResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateInventoryRequest
{
    [Range(0, 100000)]
    public int Quantity { get; set; }
}
