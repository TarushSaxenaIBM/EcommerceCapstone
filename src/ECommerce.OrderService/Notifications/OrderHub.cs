using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.OrderService.Notifications;

[Authorize]
public class OrderHub : Hub
{
}
