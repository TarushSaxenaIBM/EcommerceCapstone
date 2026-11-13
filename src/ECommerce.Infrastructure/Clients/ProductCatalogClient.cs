using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Clients;

public sealed class ProductCatalogClient : IProductCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductCatalogClient> _logger;

    public ProductCatalogClient(HttpClient httpClient, ILogger<ProductCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductSnapshot?> GetProductAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/internal/products/{productId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessAsync(response, ct);

            var product = await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions, ct);
            return product is null ? null : ToSnapshot(product);
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Product Service is unavailable while loading product {ProductId}.", productId);
            throw new ServiceUnavailableException("The product catalog is temporarily unavailable.");
        }
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetProductsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken ct = default)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<ProductSnapshot>();
        }

        var ids = string.Join(',', productIds.Distinct());

        try
        {
            using var response = await _httpClient.GetAsync($"api/internal/products?ids={ids}", ct);
            await EnsureSuccessAsync(response, ct);

            var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(JsonOptions, ct)
                ?? new List<ProductResponse>();

            return products.Select(ToSnapshot).ToList();
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Product Service is unavailable while loading {Count} products.", productIds.Count);
            throw new ServiceUnavailableException("The product catalog is temporarily unavailable.");
        }
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetFeaturedAsync(
        int take,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/internal/products/featured?take={take}", ct);
            await EnsureSuccessAsync(response, ct);

            var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>(JsonOptions, ct)
                ?? new List<ProductResponse>();

            return products.Select(ToSnapshot).ToList();
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Product Service is unavailable while loading featured products.");
            throw new ServiceUnavailableException("The product catalog is temporarily unavailable.");
        }
    }

    public async Task<bool> TryReserveStockAsync(
        int productId,
        int quantity,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/internal/inventory/{productId}/reserve",
                new { quantity },
                JsonOptions,
                ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return false;
            }

            await EnsureSuccessAsync(response, ct);
            return true;
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Product Service is unavailable while reserving stock for {ProductId}.", productId);
            throw new ServiceUnavailableException("Stock could not be reserved because the catalog is unavailable.");
        }
    }

    public async Task ReleaseStockAsync(
        int productId,
        int quantity,
        CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/internal/inventory/{productId}/release",
                new { quantity },
                JsonOptions,
                ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            await EnsureSuccessAsync(response, ct);
        }
        catch (ServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Product Service is unavailable while releasing stock for {ProductId}.", productId);
            throw new ServiceUnavailableException("Stock could not be released because the catalog is unavailable.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        throw new ServiceUnavailableException(
            $"The product catalog returned {(int)response.StatusCode}. {body}".Trim());
    }

    private static ProductSnapshot ToSnapshot(ProductResponse product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Sku,
        product.ImageUrl,
        product.CategoryId,
        product.CategoryName,
        product.IsActive,
        product.StockQuantity);
}
