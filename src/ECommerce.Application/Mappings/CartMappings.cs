using ECommerce.Application.DTOs.Cart;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mappings;

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
