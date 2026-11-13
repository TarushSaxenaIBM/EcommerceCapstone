using ECommerce.Application.DTOs.Auth;
using ECommerce.Domain.Entities;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Mappings;

public static class AuthMappings
{
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}

public static class CartMappings
{
    public static CartResponse ToResponse(this Cart cart)
    {
        var items = cart.Items.Select(ToItemResponse).ToList();

        return new CartResponse
        {
            Id = cart.Id,
            Items = items,
            TotalAmount = items.Sum(i => i.LineTotal),
            ItemCount = items.Sum(i => i.Quantity)
        };
    }

    private static CartItemResponse ToItemResponse(CartItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        ImageUrl = item.ImageUrl,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        LineTotal = Math.Round(item.UnitPrice * item.Quantity, 2),
        StockQuantity = item.StockQuantity,
        IsAvailable = item.StockQuantity >= item.Quantity
    };
}

public static class CategoryMappings
{
    public static CategoryResponse ToResponse(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive
    };
}

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

public static class ProductMappings
{
    public static ProductSummaryResponse ToSummary(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        CategoryName = product.Category?.Name ?? string.Empty,
        StockQuantity = product.Inventory?.Quantity ?? 0,
        IsActive = product.IsActive
    };

    public static ProductResponse ToResponse(this Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Sku = product.Sku,
        ImageUrl = product.ImageUrl,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        IsActive = product.IsActive,
        StockQuantity = product.Inventory?.Quantity ?? 0,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
