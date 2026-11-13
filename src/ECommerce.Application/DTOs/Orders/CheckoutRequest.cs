using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Orders;

public class CheckoutRequest
{
    [Required]
    public AddressRequest ShippingAddress { get; set; } = new();
}
