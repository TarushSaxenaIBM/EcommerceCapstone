using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Enums;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    /// <summary>Returns the signed-in user's order history.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryResponse>>> GetMyOrders(CancellationToken ct)
    {
        var result = await _orders.GetMyOrdersAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Returns all orders with optional status filtering. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryResponse>>> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _orders.GetPagedAsync(page, pageSize, status, ct);
        return Ok(result);
    }

    /// <summary>Returns one order with its items, address and payment.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id, CancellationToken ct)
    {
        var result = await _orders.GetByIdAsync(User.GetUserId(), id, User.IsInRole("Admin"), ct);
        return Ok(result);
    }

    /// <summary>Changes an order status. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        int id,
        UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        if (request.Status is null)
        {
            throw new BadRequestException("A status value is required.");
        }

        var result = await _orders.UpdateStatusAsync(id, request.Status.Value, ct);
        return Ok(result);
    }
}
