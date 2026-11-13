using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces.Services;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart)
    {
        _cart = cart;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartResponse>> GetCart(CancellationToken ct)
    {
        var result = await _cart.GetCartAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> AddItem(AddCartItemRequest request, CancellationToken ct)
    {
        var result = await _cart.AddItemAsync(User.GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPut("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> UpdateItem(
        int itemId,
        UpdateCartItemRequest request,
        CancellationToken ct)
    {
        var result = await _cart.UpdateItemAsync(User.GetUserId(), itemId, request, ct);
        return Ok(result);
    }

    [HttpDelete("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartResponse>> RemoveItem(int itemId, CancellationToken ct)
    {
        var result = await _cart.RemoveItemAsync(User.GetUserId(), itemId, ct);
        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> ClearCart(CancellationToken ct)
    {
        var result = await _cart.ClearAsync(User.GetUserId(), ct);
        return Ok(result);
    }
}
