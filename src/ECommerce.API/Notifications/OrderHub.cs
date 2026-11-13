using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Notifications;

[Authorize]
public class OrderHub : Hub
{
}
