using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Interfaces.Services;

public interface IInventoryService
{
    Task<InventoryResponse> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task<InventoryResponse> UpdateAsync(int productId, UpdateInventoryRequest request, CancellationToken ct = default);
    Task<InventoryResponse> AdjustAsync(int productId, AdjustInventoryRequest request, CancellationToken ct = default);
    Task<bool> TryReserveAsync(int productId, int quantity, CancellationToken ct = default);
    Task ReleaseAsync(int productId, int quantity, CancellationToken ct = default);
}
