using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public class ProductServiceTests
{
    private static ProductService CreateService(TestDatabase db) =>
        new(
            db.Products,
            db.Categories,
            db.ProductUnitOfWork,
            new MemoryCacheService(new MemoryCache(new MemoryCacheOptions())),
            new NullAiChatClient(),
            NullLogger<ProductService>.Instance);

    private static CreateProductRequest NewProduct(int categoryId, string? sku = null) => new()
    {
        Name = "  Canvas Tote  ",
        Description = "A simple tote bag.",
        Price = 19.999m,
        Sku = sku ?? $"TOTE-{Guid.NewGuid():N}",
        CategoryId = categoryId,
        InitialStock = 12
    };

    [Fact]
    public async Task CreateAsync_NormalizesSkuAndCreatesInventory()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync();
        var service = CreateService(db);
        var request = NewProduct(category.Id, "  tote-bag  ");

        var result = await service.CreateAsync(request);

        Assert.Equal("Canvas Tote", result.Name);
        Assert.Equal("TOTE-BAG", result.Sku);
        Assert.Equal(20.00m, result.Price);
        Assert.Equal(12, result.StockQuantity);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ThrowsConflict()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync();
        var service = CreateService(db);
        await service.CreateAsync(NewProduct(category.Id, "DUPLICATE-SKU"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(NewProduct(category.Id, "duplicate-sku")));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveCategory_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync();
        category.IsActive = false;
        await db.ProductContext.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(NewProduct(category.Id)));
    }

    [Fact]
    public async Task DeleteAsync_DeactivatesProduct()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync();
        var service = CreateService(db);
        var product = await service.CreateAsync(NewProduct(category.Id));

        await service.DeleteAsync(product.Id);

        var updated = await service.GetByIdAsync(product.Id, includeInactive: true);
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(product.Id, includeInactive: false));
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task SearchAsync_UsesKeywordFallbackWhenAiIsNotConfigured()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync("Outdoor");
        var service = CreateService(db);

        await service.CreateAsync(new CreateProductRequest
        {
            Name = "Trailhead Running Shoes",
            Description = "Lightweight shoes for daily running on mixed terrain.",
            Price = 89.95m,
            Sku = $"RUN-{Guid.NewGuid():N}",
            CategoryId = category.Id,
            InitialStock = 4
        });

        var result = await service.SearchAsync(new SemanticSearchRequest
        {
            Query = "lightweight running shoes"
        });

        Assert.False(result.UsedAi);
        Assert.Contains(result.Items, item => item.Name == "Trailhead Running Shoes");
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsMatchingNames()
    {
        await using var db = await TestDatabase.CreateAsync();
        var category = await db.AddCategoryAsync("Audio");
        var service = CreateService(db);

        await service.CreateAsync(new CreateProductRequest
        {
            Name = "Nimbus Bluetooth Speaker",
            Description = "A compact speaker for the desk.",
            Price = 59.99m,
            Sku = $"SPK-{Guid.NewGuid():N}",
            CategoryId = category.Id,
            InitialStock = 6
        });

        var suggestions = await service.GetSuggestionsAsync("nimb", 5);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal("Nimbus Bluetooth Speaker", suggestion.Name);
    }
}
