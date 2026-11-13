using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public class OrderServiceTests
{
    private sealed class RecordingPublisher : INotificationPublisher
    {
        public List<(int UserId, int OrderId, OrderStatus Status)> Messages { get; } = new();

        public Task PublishOrderStatusAsync(
            int userId,
            int orderId,
            OrderStatus status,
            CancellationToken ct = default)
        {
            Messages.Add((userId, orderId, status));
            return Task.CompletedTask;
        }
    }

    private static OrderService CreateService(TestDatabase db, INotificationPublisher publisher) =>
        new(
            db.Orders,
            db.OrderUnitOfWork,
            new TestProductCatalog(db.Products, db.Inventory),
            publisher,
            NullLogger<OrderService>.Instance);

    [Fact]
    public async Task UpdateStatus_FromConfirmedToProcessing_SavesAndPublishes()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync();
        var order = await db.SeedOrderAsync(user, product);
        var publisher = new RecordingPublisher();
        var service = CreateService(db, publisher);

        var result = await service.UpdateStatusAsync(order.Id, OrderStatus.Processing);

        Assert.Equal(OrderStatus.Processing, result.Status);
        var message = Assert.Single(publisher.Messages);
        Assert.Equal(order.Id, message.OrderId);
        Assert.Equal(OrderStatus.Processing, message.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidTransition_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync();
        var order = await db.SeedOrderAsync(user, product, OrderStatus.Pending);
        var service = CreateService(db, new RecordingPublisher());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateStatusAsync(order.Id, OrderStatus.Shipped));
    }

    [Fact]
    public async Task UpdateStatus_ToCancelled_RestocksEveryItem()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 10);
        var order = await db.SeedOrderAsync(user, product, quantity: 2);
        var service = CreateService(db, new RecordingPublisher());

        await service.UpdateStatusAsync(order.Id, OrderStatus.Cancelled);

        db.ProductContext.ChangeTracker.Clear();
        var inventory = await db.Inventory.GetByProductIdAsync(product.Id);

        Assert.NotNull(inventory);
        Assert.Equal(12, inventory.Quantity);
    }

    [Fact]
    public async Task GetById_ForAnotherUser_ThrowsNotFound()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync();
        var order = await db.SeedOrderAsync(user, product);
        var service = CreateService(db, new RecordingPublisher());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(user.Id + 1, order.Id, isAdmin: false));
    }
}
