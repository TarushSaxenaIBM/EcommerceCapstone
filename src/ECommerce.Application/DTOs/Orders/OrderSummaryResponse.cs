using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class OrderSummaryResponse
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
