using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<PaymentResponse> GetByOrderIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default);
}
