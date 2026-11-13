using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Authorize]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;

    public CheckoutController(ICheckoutService checkout)
    {
        _checkout = checkout;
    }

    /// <summary>Validates the cart, reserves stock, takes payment and creates an order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var result = await _checkout.CheckoutAsync(
            User.GetUserId(),
            User.GetUserName(),
            User.GetUserEmail(),
            request,
            ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
