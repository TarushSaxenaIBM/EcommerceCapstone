using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Payments;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly ILogger<MockPaymentProvider> _logger;

    public MockPaymentProvider(ILogger<MockPaymentProvider> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> ProcessAsync(PaymentProcessRequest request, CancellationToken ct = default)
    {
        var transactionId = $"MOCK-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Mock payment {TransactionId} processed for order {OrderId}.",
            transactionId,
            request.OrderId);

        return Task.FromResult(new PaymentResult(true, transactionId, null));
    }
}
