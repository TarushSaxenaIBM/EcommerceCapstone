using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public class CartServiceTests
{
    private static CartService CreateService(TestDatabase db) =>
        new(
            db.Carts,
            new TestProductCatalog(db.Products, db.Inventory),
            db.OrderUnitOfWork,
            NullLogger<CartService>.Instance);

    [Fact]
    public async Task AddItem_CreatesCartAndCalculatesTotals()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(price: 12.50m);
        var service = CreateService(db);

        var cart = await service.AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 2 });

        Assert.Single(cart.Items);
        Assert.Equal(2, cart.ItemCount);
        Assert.Equal(25.00m, cart.TotalAmount);
        Assert.Equal(10, cart.Items[0].StockQuantity);
    }

    [Fact]
    public async Task AddItem_WhenQuantityExceedsStock_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 3);
        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 4 }));

        Assert.Contains("Only 3", exception.Message);
    }

    [Fact]
    public async Task UpdateItem_WhenQuantityExceedsStock_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 5);
        var service = CreateService(db);
        var cart = await service.AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 1 });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateItemAsync(user.Id, cart.Items[0].Id, new() { Quantity = 6 }));
    }

    [Fact]
    public async Task RemoveItem_RemovesItFromTheCart()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync();
        var service = CreateService(db);
        var cart = await service.AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 1 });

        var updated = await service.RemoveItemAsync(user.Id, cart.Items[0].Id);

        Assert.Empty(updated.Items);
        Assert.Equal(0, updated.TotalAmount);
    }
}
