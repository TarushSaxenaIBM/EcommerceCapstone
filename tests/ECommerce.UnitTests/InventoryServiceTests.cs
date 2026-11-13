using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public class InventoryServiceTests
{
    private static InventoryService CreateService(TestDatabase db) =>
        new(
            db.Inventory,
            db.ProductUnitOfWork,
            new MemoryCacheService(new MemoryCache(new MemoryCacheOptions())),
            NullLogger<InventoryService>.Instance);

    [Fact]
    public async Task AdjustAsync_IncreasesAndDecreasesStock()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (_, product) = await db.SeedUserAndProductAsync(stock: 10);
        var service = CreateService(db);

        var increased = await service.AdjustAsync(product.Id, new() { QuantityChange = 5 });
        var decreased = await service.AdjustAsync(product.Id, new() { QuantityChange = -8 });

        Assert.Equal(15, increased.Quantity);
        Assert.Equal(7, decreased.Quantity);
    }

    [Fact]
    public async Task AdjustAsync_BelowZero_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (_, product) = await db.SeedUserAndProductAsync(stock: 3);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AdjustAsync(product.Id, new() { QuantityChange = -4 }));

        Assert.Contains("Current stock is 3", exception.Message);
    }

    [Fact]
    public async Task AdjustAsync_WithZeroChange_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (_, product) = await db.SeedUserAndProductAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AdjustAsync(product.Id, new() { QuantityChange = 0 }));
    }

    [Fact]
    public async Task UpdateAsync_SetsStockLevel()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (_, product) = await db.SeedUserAndProductAsync(stock: 2);
        var service = CreateService(db);

        var result = await service.UpdateAsync(product.Id, new() { Quantity = 40 });

        Assert.Equal(40, result.Quantity);
    }

    [Fact]
    public async Task GetByProductIdAsync_ForMissingProduct_ThrowsNotFound()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByProductIdAsync(9999));
    }

    [Fact]
    public async Task TryReserve_ReducesStockAndReleaseRestoresIt()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (_, product) = await db.SeedUserAndProductAsync(stock: 4);
        var service = CreateService(db);

        var reserved = await service.TryReserveAsync(product.Id, 3);
        var rejected = await service.TryReserveAsync(product.Id, 2);

        Assert.True(reserved);
        Assert.False(rejected);

        await service.ReleaseAsync(product.Id, 3);

        var inventory = await db.Inventory.GetByProductIdAsync(product.Id);
        Assert.Equal(4, inventory!.Quantity);
    }
}
