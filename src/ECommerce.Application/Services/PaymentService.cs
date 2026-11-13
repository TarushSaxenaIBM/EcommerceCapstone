using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;

namespace ECommerce.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _payments;

    public PaymentService(IPaymentRepository payments)
    {
        _payments = payments;
    }

    public async Task<PaymentResponse> GetByOrderIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default)
    {
        var payment = await _payments.GetByOrderIdAsync(orderId, ct);

        if (payment is null || (!isAdmin && payment.Order.UserId != userId))
        {
            throw new NotFoundException("Payment not found.");
        }

        return payment.ToResponse();
    }
}
