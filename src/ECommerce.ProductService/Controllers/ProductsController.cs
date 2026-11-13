using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    /// <summary>Returns a paged, searchable and sortable product list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductSummaryResponse>>> GetProducts(
        [FromQuery] ProductQuery query,
        CancellationToken ct)
    {
        if (query.IncludeInactive && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var result = await _products.GetPagedAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Natural-language product search with a keyword fallback.</summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SemanticSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SemanticSearchResponse>> Search(
        SemanticSearchRequest request,
        CancellationToken ct)
    {
        var result = await _products.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Returns type-ahead suggestions for a partial query.</summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSuggestionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSuggestionResponse>>> GetSuggestions(
        [FromQuery] string? query,
        [FromQuery] int take,
        CancellationToken ct)
    {
        var result = await _products.GetSuggestionsAsync(query ?? string.Empty, take == 0 ? 5 : take, ct);
        return Ok(result);
    }

    /// <summary>Returns a single product with its current stock level.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id, CancellationToken ct)
    {
        var result = await _products.GetByIdAsync(id, User.IsInRole("Admin"), ct);
        return Ok(result);
    }

    /// <summary>Creates a product. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        CreateProductRequest request,
        CancellationToken ct)
    {
        var result = await _products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    /// <summary>Updates a product. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id,
        UpdateProductRequest request,
        CancellationToken ct)
    {
        var result = await _products.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Deactivates a product. Admin only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return NoContent();
    }
}
