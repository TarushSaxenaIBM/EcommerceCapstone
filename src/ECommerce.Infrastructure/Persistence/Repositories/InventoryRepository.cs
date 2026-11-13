using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly ProductDbContext _db;

    public InventoryRepository(ProductDbContext db)
    {
        _db = db;
    }

    public Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken ct = default) =>
        _db.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public async Task AddAsync(Inventory inventory, CancellationToken ct = default) =>
        await _db.Inventories.AddAsync(inventory, ct);

    public void Update(Inventory inventory) => _db.Inventories.Update(inventory);

    public async Task<bool> TryDecrementAsync(
        int productId,
        int quantity,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var affected = await _db.Inventories
            .Where(i => i.ProductId == productId && i.Quantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.Quantity, i => i.Quantity - quantity)
                    .SetProperty(i => i.UpdatedAt, now),
                ct);

        return affected > 0;
    }

    public async Task IncrementAsync(
        int productId,
        int quantity,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Inventories
            .Where(i => i.ProductId == productId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.Quantity, i => i.Quantity + quantity)
                    .SetProperty(i => i.UpdatedAt, now),
                ct);
    }
}
