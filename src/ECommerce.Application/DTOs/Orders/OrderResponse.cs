using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class OrderResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public AddressResponse ShippingAddress { get; set; } = new();
    public IReadOnlyList<OrderItemResponse> Items { get; set; } = Array.Empty<OrderItemResponse>();
    public PaymentResponse? Payment { get; set; }
}
