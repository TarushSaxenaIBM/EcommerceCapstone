using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Controllers;

/// <summary>
/// Service-to-service catalog endpoints. They are protected by the internal API key
/// middleware and are never exposed through the public gateway.
/// </summary>
[ApiController]
[Route("api/internal/products")]
public class InternalProductsController : ControllerBase
{
    private readonly IProductService _products;

    public InternalProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken ct)
    {
        var result = await _products.GetByIdAsync(id, includeInactive: true, ct);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetMany(
        [FromQuery] string? ids,
        CancellationToken ct)
    {
        var parsed = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .Take(100)
            .ToArray();

        var result = await _products.GetByIdsAsync(parsed, ct);
        return Ok(result);
    }

    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetFeatured(
        [FromQuery] int take,
        CancellationToken ct)
    {
        var result = await _products.GetFeaturedAsync(take == 0 ? 60 : take, ct);
        return Ok(result);
    }
}
