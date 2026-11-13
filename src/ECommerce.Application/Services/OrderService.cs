using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCatalogClient _catalog;
    private readonly INotificationPublisher _notifications;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orders,
        IUnitOfWork unitOfWork,
        IProductCatalogClient catalog,
        INotificationPublisher notifications,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _catalog = catalog;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> GetMyOrdersAsync(int userId, CancellationToken ct = default)
    {
        var orders = await _orders.GetByUserIdAsync(userId, ct);
        return orders.Select(o => o.ToSummary()).ToList();
    }

    public async Task<OrderResponse> GetByIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);

        if (order is null || (!isAdmin && order.UserId != userId))
        {
            throw new NotFoundException("Order not found.");
        }

        return order.ToResponse();
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetPagedAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (orders, totalCount) = await _orders.GetPagedAsync(page, pageSize, status, ct);

        return new PagedResult<OrderSummaryResponse>
        {
            Items = orders.Select(o => o.ToSummary()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(orderId, ct)
            ?? throw new NotFoundException("Order not found.");

        if (!CanTransition(order.Status, status))
        {
            throw new BadRequestException($"An order cannot move from {order.Status} to {status}.");
        }

        if (status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                await _catalog.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
            }
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        _orders.Update(order);
        await _unitOfWork.SaveChangesAsync(ct);

        await _notifications.PublishOrderStatusAsync(order.UserId, order.Id, status, ct);

        _logger.LogInformation("Order {OrderId} status changed to {Status}.", order.Id, status);

        return order.ToResponse();
    }

    private static bool CanTransition(OrderStatus current, OrderStatus next) => (current, next) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Processing) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        (OrderStatus.Processing, OrderStatus.Shipped) => true,
        (OrderStatus.Processing, OrderStatus.Cancelled) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        _ => false
    };
}
