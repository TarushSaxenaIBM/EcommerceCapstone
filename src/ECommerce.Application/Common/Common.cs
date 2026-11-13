

namespace ECommerce.Application.Common;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ECommerce";
    public string Audience { get; set; } = "ECommerce.Client";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}

public record JwtToken(string Value, DateTime ExpiresAt);

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record ProductSnapshot(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    int StockQuantity);
