using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;

namespace ECommerce.UnitTests;

/// <summary>
/// In-process implementation of the cross-service catalog contract. It reads the
/// Product context directly, mirroring what the Product Service exposes over HTTP.
/// </summary>
internal sealed class TestProductCatalog : IProductCatalogClient
{
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public TestProductCatalog(IProductRepository products, IInventoryRepository inventory)
    {
        _products = products;
        _inventory = inventory;
    }

    public async Task<ProductSnapshot?> GetProductAsync(int productId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(productId, ct);
        return product is null ? null : ToSnapshot(product);
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken ct = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<ProductSnapshot>();
        }

        var products = await _products.GetByIdsAsync(productIds, ct);
        return products.Select(ToSnapshot).ToList();
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetFeaturedAsync(
        int take,
        CancellationToken ct = default)
    {
        var (items, _) = await _products.GetPagedAsync(
            new ProductQuery { Page = 1, PageSize = Math.Clamp(take, 1, 60), Sort = "newest" },
            ct);

        return items.Where(p => p.IsActive).Select(ToSnapshot).ToList();
    }

    public Task<bool> TryReserveStockAsync(int productId, int quantity, CancellationToken ct = default) =>
        _inventory.TryDecrementAsync(productId, quantity, ct);

    public Task ReleaseStockAsync(int productId, int quantity, CancellationToken ct = default) =>
        _inventory.IncrementAsync(productId, quantity, ct);

    private static ProductSnapshot ToSnapshot(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Sku,
        product.ImageUrl,
        product.CategoryId,
        product.Category?.Name ?? string.Empty,
        product.IsActive,
        product.Inventory?.Quantity ?? 0);
}
