using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default) =>
        await OrdersQuery()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default) =>
        OrdersQuery().FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusOlderThanAsync(
        OrderStatus status,
        DateTime createdBefore,
        int limit,
        CancellationToken ct = default) =>
        await OrdersQuery()
            .Where(o => o.Status == status && o.UpdatedAt <= createdBefore)
            .OrderBy(o => o.UpdatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetPopularProductIdsAsync(
        int take,
        CancellationToken ct = default) =>
        await _db.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(i => i.Quantity) })
            .OrderByDescending(entry => entry.Quantity)
            .Take(take)
            .Select(entry => entry.ProductId)
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _db.Orders.AddAsync(order, ct);

    public void Update(Order order) => _db.Orders.Update(order);

    private IQueryable<Order> OrdersQuery() =>
        _db.Orders
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Payment);
}
