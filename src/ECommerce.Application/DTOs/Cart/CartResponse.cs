namespace ECommerce.Application.DTOs.Cart;

public class CartResponse
{
    public int Id { get; set; }
    public IReadOnlyList<CartItemResponse> Items { get; set; } = Array.Empty<CartItemResponse>();
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}
