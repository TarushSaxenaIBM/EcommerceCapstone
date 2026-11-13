using System.ComponentModel.DataAnnotations;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus? Status { get; set; }
}
