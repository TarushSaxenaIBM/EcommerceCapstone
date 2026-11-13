using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryResponse>> GetMyOrdersAsync(int userId, CancellationToken ct = default);
    Task<OrderResponse> GetByIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryResponse>> GetPagedAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default);
}
