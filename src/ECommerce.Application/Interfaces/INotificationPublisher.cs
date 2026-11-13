using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces;

public interface INotificationPublisher
{
    Task PublishOrderStatusAsync(int userId, int orderId, OrderStatus status, CancellationToken ct = default);
}
