using System.Text.Json;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Mappings;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private static readonly TimeSpan ProductCacheDuration = TimeSpan.FromMinutes(5);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "for", "from", "good", "great", "i", "in", "is", "it",
        "me", "my", "of", "on", "or", "please", "some", "that", "the", "to", "with",
        "want", "need", "looking", "show", "find", "best", "comfortable", "cheap",
        "affordable", "budget", "daily", "use"
    };

    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IAiChatClient _ai;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IAiChatClient ai,
        ILogger<ProductService> logger)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _ai = ai;
        _logger = logger;
    }

    public async Task<PagedResult<ProductSummaryResponse>> GetPagedAsync(ProductQuery query, CancellationToken ct = default)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 50);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            query.Search = query.Search.Trim();
        }

        var (items, totalCount) = await _products.GetPagedAsync(query, ct);

        return new PagedResult<ProductSummaryResponse>
        {
            Items = items.Select(p => p.ToSummary()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductResponse> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default)
    {
        var cacheKey = $"product:{id}";
        var cached = _cache.Get<ProductResponse>(cacheKey);

        if (cached is not null)
        {
            if (!includeInactive && !cached.IsActive)
            {
                throw new NotFoundException("Product not found.");
            }

            return cached;
        }

        var product = await _products.GetByIdAsync(id, ct);

        if (product is null || (!includeInactive && !product.IsActive))
        {
            throw new NotFoundException("Product not found.");
        }

        var response = product.ToResponse();
        _cache.Set(cacheKey, response, ProductCacheDuration);

        return response;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<ProductResponse>();
        }

        var products = await _products.GetByIdsAsync(ids, ct);

        return products
            .Where(p => p.IsActive)
            .Select(p => p.ToResponse())
            .ToList();
    }

    public async Task<IReadOnlyList<ProductResponse>> GetFeaturedAsync(
        int take,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 60);

        var (items, _) = await _products.GetPagedAsync(
            new ProductQuery { Page = 1, PageSize = take, Sort = "newest" },
            ct);

        return items
            .Where(p => p.IsActive)
            .Select(p => p.ToResponse())
            .ToList();
    }

    public async Task<SemanticSearchResponse> SearchAsync(SemanticSearchRequest request, CancellationToken ct = default)
    {
        var query = request.Query.Trim();

        if (query.Length < 2)
        {
            throw new BadRequestException("Enter at least two characters to search.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var terms = Tokenize(query);
        var interpreted = string.Join(", ", terms);
        var usedAi = false;
        int? categoryId = request.CategoryId;
        decimal? minPrice = null;
        decimal? maxPrice = null;
        var sort = WantsLowPrice(query) ? "price_asc" : "newest";

        if (_ai.IsConfigured)
        {
            var interpretation = await InterpretAsync(query, ct);

            if (interpretation is not null)
            {
                terms = interpretation.Keywords;
                interpreted = string.Join(", ", terms);
                categoryId = interpretation.CategoryId ?? categoryId;
                minPrice = interpretation.MinPrice;
                maxPrice = interpretation.MaxPrice;
                usedAi = true;
            }
        }

        if (terms.Count == 0)
        {
            terms = Tokenize(query);
        }

        var productQuery = new ProductQuery
        {
            Terms = terms,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        };

        var (items, totalCount) = await _products.GetPagedAsync(productQuery, ct);

        if (totalCount == 0 && usedAi)
        {
            productQuery.Terms = Tokenize(query);
            (items, totalCount) = await _products.GetPagedAsync(productQuery, ct);
        }

        return new SemanticSearchResponse
        {
            Items = items.Select(p => p.ToSummary()).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            InterpretedQuery = interpreted,
            UsedAi = usedAi
        };
    }

    public async Task<IReadOnlyList<ProductSuggestionResponse>> GetSuggestionsAsync(
        string query,
        int take,
        CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        take = Math.Clamp(take, 1, 8);

        if (term.Length < 2)
        {
            return Array.Empty<ProductSuggestionResponse>();
        }

        var result = await _products.GetPagedAsync(
            new ProductQuery { Search = term, Page = 1, PageSize = take },
            ct);

        if (result.Items.Count == 0)
        {
            var lastWord = term.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            if (!string.IsNullOrWhiteSpace(lastWord) && lastWord.Length >= 2)
            {
                result = await _products.GetPagedAsync(
                    new ProductQuery { Terms = new[] { lastWord }, Page = 1, PageSize = take },
                    ct);
            }
        }

        return result.Items
            .Select(p => new ProductSuggestionResponse { Id = p.Id, Name = p.Name })
            .DistinctBy(p => p.Id)
            .Take(take)
            .ToList();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null || !category.IsActive)
        {
            throw new BadRequestException("The selected category does not exist or is inactive.");
        }

        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await _products.SkuExistsAsync(sku, ct: ct))
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = decimal.Round(request.Price, 2),
            Sku = sku,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            CategoryId = category.Id,
            Category = category,
            Inventory = new Inventory
            {
                Quantity = request.InitialStock,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created.", product.Id);

        return product.ToResponse();
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product not found.");

        var category = await _categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null || !category.IsActive)
        {
            throw new BadRequestException("The selected category does not exist or is inactive.");
        }

        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await _products.SkuExistsAsync(sku, id, ct))
        {
            throw new ConflictException("A product with this SKU already exists.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = decimal.Round(request.Price, 2);
        product.Sku = sku;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.CategoryId = category.Id;
        product.Category = category;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _products.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove($"product:{product.Id}");

        _logger.LogInformation("Product {ProductId} updated.", product.Id);

        return product.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Product not found.");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        _products.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);
        _cache.Remove($"product:{product.Id}");

        _logger.LogInformation("Product {ProductId} deactivated.", product.Id);
    }

    private async Task<SearchInterpretation?> InterpretAsync(string query, CancellationToken ct)
    {
        const string systemPrompt =
            "You turn shopper search queries into JSON for a product catalog. " +
            "Reply with JSON only: {\"keywords\":[\"word\"],\"category\":null,\"minPrice\":null,\"maxPrice\":null}. " +
            "Use 1-6 lowercase keywords. Use category only when the query names or clearly implies one of: " +
            "Electronics, Books, Home & Kitchen, Sports & Outdoors. Prices are numbers in Indian rupees (INR).";

        try
        {
            var raw = await _ai.CompleteAsync(systemPrompt, query, ct);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');

            if (start < 0 || end <= start)
            {
                return null;
            }

            using var document = JsonDocument.Parse(raw[start..(end + 1)]);
            var root = document.RootElement;

            var keywords = root.TryGetProperty("keywords", out var keywordElement) &&
                           keywordElement.ValueKind == JsonValueKind.Array
                ? keywordElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!.Trim().ToLowerInvariant())
                    .Where(e => e.Length >= 2)
                    .Distinct()
                    .Take(6)
                    .ToList()
                : new List<string>();

            if (keywords.Count == 0)
            {
                return null;
            }

            int? categoryId = null;
            var categoryName = root.TryGetProperty("category", out var categoryElement) &&
                               categoryElement.ValueKind == JsonValueKind.String
                ? categoryElement.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var categories = await _categories.GetAllAsync(includeInactive: false, ct);
                categoryId = categories
                    .FirstOrDefault(c => string.Equals(c.Name, categoryName.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?.Id;
            }

            return new SearchInterpretation(
                keywords,
                categoryId,
                ReadPrice(root, "minPrice"),
                ReadPrice(root, "maxPrice"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not interpret the search query with the AI provider.");
            return null;
        }
    }

    private static decimal? ReadPrice(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) &&
        element.ValueKind == JsonValueKind.Number &&
        element.TryGetDecimal(out var value) &&
        value > 0
            ? value
            : null;

    private static List<string> Tokenize(string query) =>
        query.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Trim().ToLowerInvariant())
            .Where(word => word.Length >= 2 && !StopWords.Contains(word))
            .Distinct()
            .Take(8)
            .ToList();

    private static bool WantsLowPrice(string query) =>
        query.Contains("cheap", StringComparison.OrdinalIgnoreCase) ||
        query.Contains("affordable", StringComparison.OrdinalIgnoreCase) ||
        query.Contains("budget", StringComparison.OrdinalIgnoreCase);

    private sealed record SearchInterpretation(
        List<string> Keywords,
        int? CategoryId,
        decimal? MinPrice,
        decimal? MaxPrice);
}
