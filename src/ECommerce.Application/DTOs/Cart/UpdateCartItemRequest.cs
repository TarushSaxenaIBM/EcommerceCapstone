using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Cart;

public class UpdateCartItemRequest
{
    [Range(1, 1000)]
    public int Quantity { get; set; }
}
