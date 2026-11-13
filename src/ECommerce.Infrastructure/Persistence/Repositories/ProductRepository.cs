using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _db;

    public ProductRepository(ProductDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQuery query,
        CancellationToken ct = default)
    {
        var products = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .AsQueryable();

        if (!query.IncludeInactive)
        {
            products = products.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            products = products.Where(p =>
                EF.Functions.Like(p.Name, pattern) ||
                EF.Functions.Like(p.Description, pattern) ||
                EF.Functions.Like(p.Sku, pattern));
        }

        if (query.Terms is { Count: > 0 })
        {
            foreach (var term in query.Terms.Where(t => !string.IsNullOrWhiteSpace(t)).Take(8))
            {
                var pattern = $"%{term.Trim()}%";
                products = products.Where(p =>
                    EF.Functions.Like(p.Name, pattern) ||
                    EF.Functions.Like(p.Description, pattern) ||
                    EF.Functions.Like(p.Category.Name, pattern) ||
                    EF.Functions.Like(p.Sku, pattern));
            }
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            products = products.Where(p => p.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= query.MaxPrice.Value);
        }

        products = query.Sort switch
        {
            "price_asc" => products.OrderBy(p => p.Price).ThenBy(p => p.Name),
            "price_desc" => products.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
            "name" => products.OrderBy(p => p.Name),
            "oldest" => products.OrderBy(p => p.CreatedAt),
            _ => products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var totalCount = await products.CountAsync(ct);
        var items = await products
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Products
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default) =>
        await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventory)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

    public Task<bool> SkuExistsAsync(
        string sku,
        int? excludeProductId = null,
        CancellationToken ct = default) =>
        _db.Products.AnyAsync(
            p => p.Sku == sku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value),
            ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _db.Products.AddAsync(product, ct);

    public void Update(Product product) => _db.Products.Update(product);
}
