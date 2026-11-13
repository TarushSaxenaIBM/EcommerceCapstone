using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Controllers;

public class ReserveStockRequest
{
    [Range(1, 1000)]
    public int Quantity { get; set; }
}

[ApiController]
[Route("api/internal/inventory")]
public class InternalInventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InternalInventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpPost("{productId:int}/reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reserve(
        int productId,
        ReserveStockRequest request,
        CancellationToken ct)
    {
        var reserved = await _inventory.TryReserveAsync(productId, request.Quantity, ct);

        if (!reserved)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Insufficient stock",
                Detail = "There is not enough stock to reserve this quantity."
            });
        }

        return Ok(new { productId, reserved = request.Quantity });
    }

    [HttpPost("{productId:int}/release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(
        int productId,
        ReserveStockRequest request,
        CancellationToken ct)
    {
        await _inventory.ReleaseAsync(productId, request.Quantity, ct);
        return NoContent();
    }
}
