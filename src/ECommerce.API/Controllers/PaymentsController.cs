using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    /// <summary>Returns the payment for an order owned by the current user (or any order for admins).</summary>
    [HttpGet("{orderId:int}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetPayment(int orderId, CancellationToken ct)
    {
        var result = await _payments.GetByOrderIdAsync(User.GetUserId(), orderId, User.IsInRole("Admin"), ct);
        return Ok(result);
    }
}
