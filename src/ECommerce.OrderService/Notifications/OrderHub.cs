using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.OrderService.Notifications;

/// <summary>
/// User-scoped order notifications. The client connects with the JWT in the
/// <c>access_token</c> query string because WebSockets cannot send headers.
/// </summary>
[Authorize]
public class OrderHub : Hub
{
}
