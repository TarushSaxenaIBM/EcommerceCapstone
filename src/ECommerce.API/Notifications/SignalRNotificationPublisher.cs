using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Notifications;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<OrderHub> _hub;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(IHubContext<OrderHub> hub, ILogger<SignalRNotificationPublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishOrderStatusAsync(int userId, int orderId, OrderStatus status, CancellationToken ct = default)
    {
        await _hub.Clients.User(userId.ToString()).SendAsync(
            "orderStatusChanged",
            new { orderId, status = status.ToString() },
            ct);

        _logger.LogInformation(
            "Sent order {OrderId} status {Status} to user {UserId}.",
            orderId,
            status,
            userId);
    }
}
