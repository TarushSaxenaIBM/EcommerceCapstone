using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class CheckoutService : ICheckoutService
{
    private readonly ICartRepository _carts;
    private readonly IAddressRepository _addresses;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCatalogClient _catalog;
    private readonly IPaymentProvider _paymentProvider;
    private readonly INotificationPublisher _notifications;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ICartRepository carts,
        IAddressRepository addresses,
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IProductCatalogClient catalog,
        IPaymentProvider paymentProvider,
        INotificationPublisher notifications,
        ILogger<CheckoutService> logger)
    {
        _carts = carts;
        _addresses = addresses;
        _orders = orders;
        _unitOfWork = unitOfWork;
        _catalog = catalog;
        _paymentProvider = paymentProvider;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<OrderResponse> CheckoutAsync(
        int userId,
        string customerName,
        string customerEmail,
        CheckoutRequest request,
        CancellationToken ct = default)
    {
        var reserved = new List<(int ProductId, int Quantity)>();

        Order order;

        try
        {
            order = await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var cart = await _carts.GetByUserIdAsync(userId, token);

                if (cart is null || cart.Items.Count == 0)
                {
                    throw new BadRequestException("Your cart is empty.");
                }

                var products = (await _catalog.GetProductsAsync(
                    cart.Items.Select(i => i.ProductId).Distinct().ToList(),
                    token)).ToDictionary(p => p.Id);

                var orderItems = new List<OrderItem>();
                decimal total = 0;

                foreach (var cartItem in cart.Items)
                {
                    if (!products.TryGetValue(cartItem.ProductId, out var product) || !product.IsActive)
                    {
                        throw new ConflictException("One of the products in your cart is no longer available.");
                    }

                    if (cartItem.Quantity > product.StockQuantity)
                    {
                        throw new ConflictException($"Only {product.StockQuantity} unit(s) of '{product.Name}' are available.");
                    }

                    var lineTotal = decimal.Round(product.Price * cartItem.Quantity, 2);

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = lineTotal
                    });

                    total += lineTotal;
                }

                var address = new Address
                {
                    UserId = userId,
                    FullName = request.ShippingAddress.FullName.Trim(),
                    AddressLine = request.ShippingAddress.AddressLine.Trim(),
                    City = request.ShippingAddress.City.Trim(),
                    State = request.ShippingAddress.State.Trim(),
                    PostalCode = request.ShippingAddress.PostalCode.Trim(),
                    Country = request.ShippingAddress.Country.Trim(),
                    PhoneNumber = request.ShippingAddress.PhoneNumber.Trim()
                };

                await _addresses.AddAsync(address, token);

                var newOrder = new Order
                {
                    UserId = userId,
                    CustomerName = customerName.Trim(),
                    CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
                    TotalAmount = decimal.Round(total, 2),
                    Status = OrderStatus.Pending,
                    ShippingAddress = address,
                    Items = orderItems,
                    Payment = new Payment
                    {
                        Amount = decimal.Round(total, 2),
                        Status = PaymentStatus.Pending
                    }
                };

                await _orders.AddAsync(newOrder, token);
                await _unitOfWork.SaveChangesAsync(token);

                foreach (var item in orderItems)
                {
                    if (!await _catalog.TryReserveStockAsync(item.ProductId, item.Quantity, token))
                    {
                        throw new ConflictException($"Not enough stock for '{item.ProductName}'.");
                    }

                    reserved.Add((item.ProductId, item.Quantity));
                }

                var paymentResult = await _paymentProvider.ProcessAsync(
                    new PaymentProcessRequest(newOrder.Id, newOrder.TotalAmount, customerEmail),
                    token);

                if (!paymentResult.Success)
                {
                    _logger.LogWarning(
                        "Payment failed for order {OrderId}: {Reason}",
                        newOrder.Id,
                        paymentResult.ErrorMessage ?? "Unknown reason");

                    throw new ConflictException(paymentResult.ErrorMessage ?? "The payment could not be processed.");
                }

                newOrder.Payment!.Status = PaymentStatus.Successful;
                newOrder.Payment.TransactionId = paymentResult.TransactionId;
                newOrder.Payment.UpdatedAt = DateTime.UtcNow;
                newOrder.Status = OrderStatus.Confirmed;
                newOrder.UpdatedAt = DateTime.UtcNow;

                cart.Items.Clear();
                cart.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(token);

                return newOrder;
            }, ct);
        }
        catch
        {
            await ReleaseReservedAsync(reserved, ct);
            throw;
        }

        try
        {
            await _notifications.PublishOrderStatusAsync(userId, order.Id, OrderStatus.Confirmed, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not publish notification for order {OrderId}.", order.Id);
        }

        _logger.LogInformation("Order {OrderId} created for user {UserId}.", order.Id, userId);

        return order.ToResponse();
    }

    private async Task ReleaseReservedAsync(List<(int ProductId, int Quantity)> reserved, CancellationToken ct)
    {
        foreach (var (productId, quantity) in reserved)
        {
            try
            {
                await _catalog.ReleaseStockAsync(productId, quantity, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not release {Quantity} unit(s) of product {ProductId} after a failed checkout.",
                    quantity,
                    productId);
            }
        }
    }
}
