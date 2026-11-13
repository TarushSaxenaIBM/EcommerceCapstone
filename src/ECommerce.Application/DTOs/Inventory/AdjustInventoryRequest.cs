using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Inventory;

public class AdjustInventoryRequest
{
    [Range(-100000, 100000)]
    public int QuantityChange { get; set; }
}
