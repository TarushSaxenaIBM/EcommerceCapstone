using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventory,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<InventoryService> logger)
    {
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<InventoryResponse> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        var inventory = await _inventory.GetByProductIdAsync(productId, ct)
            ?? throw new NotFoundException("Inventory record not found for this product.");

        return inventory.ToResponse();
    }

    public async Task<InventoryResponse> UpdateAsync(int productId, UpdateInventoryRequest request, CancellationToken ct = default)
    {
        var inventory = await _inventory.GetByProductIdAsync(productId, ct)
            ?? throw new NotFoundException("Inventory record not found for this product.");

        inventory.Quantity = request.Quantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        _inventory.Update(inventory);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove($"product:{productId}");

        _logger.LogInformation("Stock for product {ProductId} set to {Quantity}.", productId, inventory.Quantity);

        return inventory.ToResponse();
    }

    public async Task<InventoryResponse> AdjustAsync(int productId, AdjustInventoryRequest request, CancellationToken ct = default)
    {
        if (request.QuantityChange == 0)
        {
            throw new BadRequestException("Quantity change must not be zero.");
        }

        var inventory = await _inventory.GetByProductIdAsync(productId, ct)
            ?? throw new NotFoundException("Inventory record not found for this product.");

        var newQuantity = inventory.Quantity + request.QuantityChange;

        if (newQuantity < 0)
        {
            throw new BadRequestException($"Cannot reduce stock below zero. Current stock is {inventory.Quantity}.");
        }

        inventory.Quantity = newQuantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        _inventory.Update(inventory);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove($"product:{productId}");

        _logger.LogInformation(
            "Stock for product {ProductId} adjusted by {Change}. New quantity: {Quantity}.",
            productId,
            request.QuantityChange,
            inventory.Quantity);

        return inventory.ToResponse();
    }

    public async Task<bool> TryReserveAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException("Quantity must be greater than zero.");
        }

        var reserved = await _inventory.TryDecrementAsync(productId, quantity, ct);

        if (reserved)
        {
            _cache.Remove($"product:{productId}");
            _logger.LogInformation("Reserved {Quantity} unit(s) of product {ProductId}.", quantity, productId);
        }

        return reserved;
    }

    public async Task ReleaseAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new BadRequestException("Quantity must be greater than zero.");
        }

        await _inventory.IncrementAsync(productId, quantity, ct);
        _cache.Remove($"product:{productId}");

        _logger.LogInformation("Released {Quantity} unit(s) of product {ProductId}.", quantity, productId);
    }
}
