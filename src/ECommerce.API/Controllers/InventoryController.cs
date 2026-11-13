using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory)
    {
        _inventory = inventory;
    }

    /// <summary>Returns the stock record for a product. Admin only.</summary>
    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>> GetInventory(int productId, CancellationToken ct)
    {
        var result = await _inventory.GetByProductIdAsync(productId, ct);
        return Ok(result);
    }

    /// <summary>Sets the stock level for a product. Admin only.</summary>
    [HttpPut("{productId:int}")]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>> UpdateInventory(
        int productId,
        UpdateInventoryRequest request,
        CancellationToken ct)
    {
        var result = await _inventory.UpdateAsync(productId, request, ct);
        return Ok(result);
    }

    /// <summary>Increases or decreases stock by a relative amount. Admin only.</summary>
    [HttpPost("{productId:int}/adjust")]
    [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>> AdjustInventory(
        int productId,
        AdjustInventoryRequest request,
        CancellationToken ct)
    {
        var result = await _inventory.AdjustAsync(productId, request, ct);
        return Ok(result);
    }
}
