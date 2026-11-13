namespace ECommerce.Application.Interfaces;

public record PaymentProcessRequest(int OrderId, decimal Amount, string CustomerEmail);

public record PaymentResult(bool Success, string? TransactionId, string? ErrorMessage);

public interface IPaymentProvider
{
    Task<PaymentResult> ProcessAsync(PaymentProcessRequest request, CancellationToken ct = default);
}
