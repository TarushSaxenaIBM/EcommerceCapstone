using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Interfaces.Services;

public interface ICheckoutService
{
    Task<OrderResponse> CheckoutAsync(
        int userId,
        string customerName,
        string customerEmail,
        CheckoutRequest request,
        CancellationToken ct = default);
}
