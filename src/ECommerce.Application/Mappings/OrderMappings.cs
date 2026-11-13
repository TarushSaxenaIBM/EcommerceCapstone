using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        CustomerName = order.CustomerName,
        CustomerEmail = order.CustomerEmail,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        ShippingAddress = order.ShippingAddress is null
            ? new AddressResponse()
            : new AddressResponse
            {
                FullName = order.ShippingAddress.FullName,
                AddressLine = order.ShippingAddress.AddressLine,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                PostalCode = order.ShippingAddress.PostalCode,
                Country = order.ShippingAddress.Country,
                PhoneNumber = order.ShippingAddress.PhoneNumber
            },
        Items = order.Items.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList(),
        Payment = order.Payment?.ToResponse()
    };

    public static OrderSummaryResponse ToSummary(this Order order) => new()
    {
        Id = order.Id,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        ItemCount = order.Items.Sum(i => i.Quantity),
        CreatedAt = order.CreatedAt
    };

    public static PaymentResponse ToResponse(this Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Status = payment.Status,
        TransactionId = payment.TransactionId,
        CreatedAt = payment.CreatedAt
    };
}
