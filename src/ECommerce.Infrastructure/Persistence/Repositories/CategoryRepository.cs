using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _db;

    public CategoryRepository(ProductDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(
        string name,
        int? excludeCategoryId = null,
        CancellationToken ct = default)
    {
        var normalized = name.ToLower();

        return _db.Categories.AnyAsync(
            c => c.Name.ToLower() == normalized &&
                 (!excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value),
            ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await _db.Categories.AddAsync(category, ct);

    public void Update(Category category) => _db.Categories.Update(category);
}
