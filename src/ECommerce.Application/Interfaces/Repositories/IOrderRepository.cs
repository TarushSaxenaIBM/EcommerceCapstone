using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByStatusOlderThanAsync(OrderStatus status, DateTime createdBefore, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetPopularProductIdsAsync(int take, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
}
