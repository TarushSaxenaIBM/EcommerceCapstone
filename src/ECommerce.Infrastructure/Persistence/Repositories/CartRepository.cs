using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly OrderDbContext _db;

    public CartRepository(OrderDbContext db)
    {
        _db = db;
    }

    public Task<Cart?> GetByUserIdAsync(int userId, CancellationToken ct = default) =>
        _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task AddAsync(Cart cart, CancellationToken ct = default) =>
        await _db.Carts.AddAsync(cart, ct);
}
