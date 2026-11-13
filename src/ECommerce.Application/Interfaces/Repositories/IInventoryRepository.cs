using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task AddAsync(Inventory inventory, CancellationToken ct = default);
    void Update(Inventory inventory);
    Task<bool> TryDecrementAsync(int productId, int quantity, CancellationToken ct = default);
    Task IncrementAsync(int productId, int quantity, CancellationToken ct = default);
}
