using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly OrderDbContext _db;

    public PaymentRepository(OrderDbContext db)
    {
        _db = db;
    }

    public Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) =>
        _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public async Task AddAsync(Payment payment, CancellationToken ct = default) =>
        await _db.Payments.AddAsync(payment, ct);

    public void Update(Payment payment) => _db.Payments.Update(payment);
}
