using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public class CheckoutServiceTests
{
    private sealed class FakePaymentProvider : IPaymentProvider
    {
        private readonly bool _success;

        public FakePaymentProvider(bool success)
        {
            _success = success;
        }

        public Task<PaymentResult> ProcessAsync(PaymentProcessRequest request, CancellationToken ct = default) =>
            Task.FromResult(_success
                ? new PaymentResult(true, "TEST-TXN", null)
                : new PaymentResult(false, null, "Card declined."));
    }

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

    private static TestProductCatalog Catalog(TestDatabase db) =>
        new(db.Products, db.Inventory);

    private static CheckoutService CreateService(
        TestDatabase db,
        IPaymentProvider payments,
        INotificationPublisher publisher) =>
        new(
            db.Carts,
            db.Addresses,
            db.Orders,
            db.OrderUnitOfWork,
            Catalog(db),
            payments,
            publisher,
            NullLogger<CheckoutService>.Instance);

    private static CartService CreateCartService(TestDatabase db) =>
        new(db.Carts, Catalog(db), db.OrderUnitOfWork, NullLogger<CartService>.Instance);

    private static CheckoutRequest Request() => new()
    {
        ShippingAddress = new AddressRequest
        {
            FullName = "Test Shopper",
            AddressLine = "1 Test Street",
            City = "Testville",
            State = "TS",
            PostalCode = "12345",
            Country = "USA",
            PhoneNumber = "5551234567"
        }
    };

    [Fact]
    public async Task Checkout_CreatesConfirmedOrderChargesStockAndClearsCart()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 5, price: 12.50m);
        await CreateCartService(db).AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 2 });
        var publisher = new RecordingPublisher();
        var service = CreateService(db, new FakePaymentProvider(success: true), publisher);

        var result = await service.CheckoutAsync(
            user.Id,
            "Test Shopper",
            "shopper@example.com",
            Request());

        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Equal("Test Shopper", result.CustomerName);
        Assert.Equal("shopper@example.com", result.CustomerEmail);
        Assert.Equal(25.00m, result.TotalAmount);
        Assert.NotNull(result.Payment);
        Assert.Equal(PaymentStatus.Successful, result.Payment.Status);
        Assert.Equal("TEST-TXN", result.Payment.TransactionId);

        db.OrderContext.ChangeTracker.Clear();
        db.ProductContext.ChangeTracker.Clear();

        var cart = await db.Carts.GetByUserIdAsync(user.Id);
        var inventory = await db.Inventory.GetByProductIdAsync(product.Id);

        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(3, inventory!.Quantity);
        var message = Assert.Single(publisher.Messages);
        Assert.Equal(OrderStatus.Confirmed, message.Status);
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_ThrowsBadRequest()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, _) = await db.SeedUserAndProductAsync();
        var service = CreateService(db, new FakePaymentProvider(success: true), new RecordingPublisher());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CheckoutAsync(user.Id, "Test Shopper", "shopper@example.com", Request()));
    }

    [Fact]
    public async Task Checkout_WhenStockFallsBelowCartQuantity_ThrowsConflict()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 5);
        await CreateCartService(db).AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 2 });

        var inventory = await db.Inventory.GetByProductIdAsync(product.Id);
        inventory!.Quantity = 1;
        db.Inventory.Update(inventory);
        await db.ProductUnitOfWork.SaveChangesAsync();
        db.ProductContext.ChangeTracker.Clear();

        var service = CreateService(db, new FakePaymentProvider(success: true), new RecordingPublisher());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CheckoutAsync(user.Id, "Test Shopper", "shopper@example.com", Request()));

        Assert.Contains("Only 1", exception.Message);
    }

    [Fact]
    public async Task Checkout_WhenPaymentFails_ThrowsConflictAndKeepsStock()
    {
        await using var db = await TestDatabase.CreateAsync();
        var (user, product) = await db.SeedUserAndProductAsync(stock: 5);
        await CreateCartService(db).AddItemAsync(user.Id, new() { ProductId = product.Id, Quantity = 1 });

        var publisher = new RecordingPublisher();
        var service = CreateService(db, new FakePaymentProvider(success: false), publisher);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CheckoutAsync(user.Id, "Test Shopper", "shopper@example.com", Request()));

        Assert.Contains("Card declined", exception.Message);

        db.ProductContext.ChangeTracker.Clear();
        var inventory = await db.Inventory.GetByProductIdAsync(product.Id);

        Assert.Equal(5, inventory!.Quantity);
        Assert.Empty(publisher.Messages);
    }
}
