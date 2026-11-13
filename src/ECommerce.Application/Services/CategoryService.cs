using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private const string ActiveCategoriesCacheKey = "categories:active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<CategoryService> logger)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        if (!includeInactive)
        {
            var cached = _cache.Get<IReadOnlyList<CategoryResponse>>(ActiveCategoriesCacheKey);
            if (cached is not null)
            {
                return cached;
            }
        }

        var categories = await _categories.GetAllAsync(includeInactive, ct);
        var response = categories.Select(c => c.ToResponse()).ToList();

        if (!includeInactive)
        {
            _cache.Set(ActiveCategoriesCacheKey, response, CacheDuration);
        }

        return response;
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);

        if (category is null || (!includeInactive && !category.IsActive))
        {
            throw new NotFoundException("Category not found.");
        }

        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _categories.NameExistsAsync(name, ct: ct))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description.Trim()
        };

        await _categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove(ActiveCategoriesCacheKey);

        _logger.LogInformation("Category {CategoryId} created.", category.Id);

        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Category not found.");

        var name = request.Name.Trim();

        if (await _categories.NameExistsAsync(name, id, ct))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        category.Name = name;
        category.Description = request.Description.Trim();
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove(ActiveCategoriesCacheKey);

        _logger.LogInformation("Category {CategoryId} updated.", category.Id);

        return category.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Category not found.");

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove(ActiveCategoriesCacheKey);

        _logger.LogInformation("Category {CategoryId} deactivated.", category.Id);
    }
}
