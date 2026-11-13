using System.Text.Json;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Recommendations;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class RecommendationService : IRecommendationService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const int MaxItems = 6;

    private readonly IOrderRepository _orders;
    private readonly IProductCatalogClient _catalog;
    private readonly ICacheService _cache;
    private readonly IAiChatClient _ai;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        IOrderRepository orders,
        IProductCatalogClient catalog,
        ICacheService cache,
        IAiChatClient ai,
        ILogger<RecommendationService> logger)
    {
        _orders = orders;
        _catalog = catalog;
        _cache = cache;
        _ai = ai;
        _logger = logger;
    }

    public async Task<RecommendationResponse> GetHomeAsync(int? userId, CancellationToken ct = default)
    {
        var cacheKey = $"recommendations:home:{userId ?? 0}";
        var cached = _cache.Get<RecommendationResponse>(cacheKey);

        if (cached is not null)
        {
            return cached;
        }

        var catalog = await _catalog.GetFeaturedAsync(60);

        if (catalog.Count == 0)
        {
            return new RecommendationResponse();
        }

        var purchasedIds = new HashSet<int>();
        var preferredCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (userId.HasValue)
        {
            var orders = await _orders.GetByUserIdAsync(userId.Value, ct);
            purchasedIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).ToHashSet();

            foreach (var product in catalog.Where(p => purchasedIds.Contains(p.Id)))
            {
                preferredCategories.Add(product.CategoryName);
            }
        }

        var popularIds = await _orders.GetPopularProductIdsAsync(20, ct);
        var popularity = popularIds
            .Select((id, index) => (id, index))
            .ToDictionary(entry => entry.id, entry => entry.index);

        var candidates = catalog
            .Where(p => p.IsActive && p.StockQuantity > 0 && !purchasedIds.Contains(p.Id))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = catalog.Where(p => p.IsActive && p.StockQuantity > 0).ToList();
        }

        candidates = candidates
            .OrderByDescending(p => preferredCategories.Contains(p.CategoryName))
            .ThenBy(p => popularity.TryGetValue(p.Id, out var rank) ? rank : int.MaxValue)
            .ThenByDescending(p => p.StockQuantity)
            .ThenBy(p => p.Price)
            .ToList();

        var personalized = purchasedIds.Count > 0;
        var aiRanked = false;

        if (_ai.IsConfigured)
        {
            var ranked = await RankWithAiAsync(
                candidates,
                "Recommend products for a shopper homepage. Prefer variety and relevant categories.",
                ct);

            if (ranked.Count > 0)
            {
                candidates = ranked;
                aiRanked = true;
            }
        }

        var items = candidates.Take(MaxItems).Select(product =>
            ToResponse(
                product,
                preferredCategories.Contains(product.CategoryName)
                    ? $"More in {product.CategoryName}"
                    : popularity.ContainsKey(product.Id)
                        ? "Popular with other shoppers"
                        : "A good place to start")).ToList();

        var response = new RecommendationResponse
        {
            Items = items,
            Personalized = personalized,
            AiRanked = aiRanked
        };

        _cache.Set(cacheKey, response, CacheDuration);

        return response;
    }

    public async Task<RecommendationResponse> GetRelatedAsync(int productId, int? userId, CancellationToken ct = default)
    {
        var cacheKey = $"recommendations:related:{productId}";
        var cached = _cache.Get<RecommendationResponse>(cacheKey);

        if (cached is not null)
        {
            return cached;
        }

        var product = await _catalog.GetProductAsync(productId, ct)
            ?? throw new NotFoundException("Product not found.");

        var catalog = await _catalog.GetFeaturedAsync(60);
        var popularIds = await _orders.GetPopularProductIdsAsync(20, ct);
        var popularity = popularIds
            .Select((id, index) => (id, index))
            .ToDictionary(entry => entry.id, entry => entry.index);

        var sameCategory = catalog
            .Where(p => p.IsActive && p.StockQuantity > 0 && p.Id != productId &&
                        string.Equals(p.CategoryName, product.CategoryName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => popularity.TryGetValue(p.Id, out var rank) ? rank : int.MaxValue)
            .ToList();

        var candidates = new List<ProductSnapshot>(sameCategory);

        if (candidates.Count < MaxItems)
        {
            candidates.AddRange(catalog
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.Id != productId &&
                            candidates.All(c => c.Id != p.Id))
                .OrderBy(p => popularity.TryGetValue(p.Id, out var rank) ? rank : int.MaxValue));
        }

        var aiRanked = false;

        if (_ai.IsConfigured)
        {
            var ranked = await RankWithAiAsync(
                candidates,
                $"Recommend products related to '{product.Name}' for a product detail page.",
                ct);

            if (ranked.Count > 0)
            {
                candidates = ranked;
                aiRanked = true;
            }
        }

        var items = candidates.Take(MaxItems).Select(p => ToResponse(
            p,
            string.Equals(p.CategoryName, product.CategoryName, StringComparison.OrdinalIgnoreCase)
                ? "Customers also bought"
                : "You may also like")).ToList();

        var response = new RecommendationResponse
        {
            Items = items,
            Personalized = userId.HasValue,
            AiRanked = aiRanked
        };

        _cache.Set(cacheKey, response, CacheDuration);

        return response;
    }

    private async Task<List<ProductSnapshot>> RankWithAiAsync(
        List<ProductSnapshot> candidates,
        string goal,
        CancellationToken ct)
    {
        if (candidates.Count <= MaxItems)
        {
            return candidates;
        }

        try
        {
            var payload = candidates.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                category = p.CategoryName,
                price = p.Price
            });

            var systemPrompt =
                "You rank a small e-commerce catalog. " +
                "Reply with a JSON array of product ids only, most relevant first, maximum 6. " +
                "Do not include any other text.";

            var userPrompt = goal + "\nCatalog: " + JsonSerializer.Serialize(payload);
            var raw = await _ai.CompleteAsync(systemPrompt, userPrompt, ct);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return candidates;
            }

            var start = raw.IndexOf('[');
            var end = raw.LastIndexOf(']');

            if (start < 0 || end <= start)
            {
                return candidates;
            }

            using var document = JsonDocument.Parse(raw[start..(end + 1)]);
            var byId = candidates.ToDictionary(p => p.Id);

            var ranked = document.RootElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                .Select(e => e.GetInt32())
                .Distinct()
                .Where(byId.ContainsKey)
                .Select(id => byId[id])
                .ToList();

            return ranked.Count >= 3 ? ranked : candidates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI recommendation ranking failed; using the heuristic order.");
            return candidates;
        }
    }

    private static RecommendedProductResponse ToResponse(ProductSnapshot product, string reason) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        CategoryName = product.CategoryName,
        StockQuantity = product.StockQuantity,
        Reason = reason
    };
}
