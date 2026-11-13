using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Inventory;

public class UpdateInventoryRequest
{
    [Range(0, 100000)]
    public int Quantity { get; set; }
}
