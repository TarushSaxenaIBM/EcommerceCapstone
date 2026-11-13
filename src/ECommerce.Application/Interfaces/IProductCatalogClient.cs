using ECommerce.Application.Common;

namespace ECommerce.Application.Interfaces;

public interface IProductCatalogClient
{
    Task<ProductSnapshot?> GetProductAsync(int productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductSnapshot>> GetProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductSnapshot>> GetFeaturedAsync(int take, CancellationToken ct = default);

    Task<bool> TryReserveStockAsync(int productId, int quantity, CancellationToken ct = default);

    Task ReleaseStockAsync(int productId, int quantity, CancellationToken ct = default);
}
