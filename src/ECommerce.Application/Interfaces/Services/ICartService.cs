using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces.Services;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(int userId, CancellationToken ct = default);
    Task<CartResponse> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct = default);
    Task<CartResponse> UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<CartResponse> RemoveItemAsync(int userId, int itemId, CancellationToken ct = default);
    Task<CartResponse> ClearAsync(int userId, CancellationToken ct = default);
}
