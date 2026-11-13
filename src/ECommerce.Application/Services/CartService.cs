using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IProductCatalogClient _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        ICartRepository carts,
        IProductCatalogClient catalog,
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger)
    {
        _carts = carts;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CartResponse> GetCartAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        await RefreshSnapshotsAsync(cart, ct);
        return cart.ToResponse();
    }

    public async Task<CartResponse> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct = default)
    {
        var product = await _catalog.GetProductAsync(request.ProductId, ct);

        if (product is null || !product.IsActive)
        {
            throw new NotFoundException("Product not found.");
        }

        var cart = await GetOrCreateCartAsync(userId, ct);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
        var newQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

        if (newQuantity > product.StockQuantity)
        {
            throw new BadRequestException($"Only {product.StockQuantity} unit(s) of '{product.Name}' are available.");
        }

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                UnitPrice = product.Price,
                Quantity = request.Quantity,
                StockQuantity = product.StockQuantity
            });
        }
        else
        {
            existingItem.Quantity = newQuantity;
            ApplySnapshot(existingItem, product);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} added to cart for user {UserId}.", product.Id, userId);

        return cart.ToResponse();
    }

    public async Task<CartResponse> UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");

        var product = await _catalog.GetProductAsync(item.ProductId, ct);

        if (product is null || !product.IsActive)
        {
            throw new BadRequestException("This product is no longer available.");
        }

        if (request.Quantity > product.StockQuantity)
        {
            throw new BadRequestException($"Only {product.StockQuantity} unit(s) of '{product.Name}' are available.");
        }

        item.Quantity = request.Quantity;
        ApplySnapshot(item, product);
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return cart.ToResponse();
    }

    public async Task<CartResponse> RemoveItemAsync(int userId, int itemId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return cart.ToResponse();
    }

    public async Task<CartResponse> ClearAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Cart cleared for user {UserId}.", userId);

        return cart.ToResponse();
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId, CancellationToken ct)
    {
        var cart = await _carts.GetByUserIdAsync(userId, ct);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        await _carts.AddAsync(cart, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return cart;
    }

    private async Task RefreshSnapshotsAsync(Cart cart, CancellationToken ct)
    {
        if (cart.Items.Count == 0)
        {
            return;
        }

        try
        {
            var products = (await _catalog.GetProductsAsync(
                cart.Items.Select(i => i.ProductId).Distinct().ToList(),
                ct)).ToDictionary(p => p.Id);

            foreach (var item in cart.Items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    ApplySnapshot(item, product);
                }
            }
        }
        catch (ServiceUnavailableException ex)
        {
            // The cart can still be displayed from the last known snapshots.
            _logger.LogWarning(ex, "Could not refresh product snapshots for user {UserId}.", cart.UserId);
        }
    }

    private static void ApplySnapshot(CartItem item, Common.ProductSnapshot product)
    {
        item.ProductName = product.Name;
        item.ImageUrl = product.ImageUrl;
        item.UnitPrice = product.Price;
        item.StockQuantity = product.IsActive ? product.StockQuantity : 0;
    }
}
