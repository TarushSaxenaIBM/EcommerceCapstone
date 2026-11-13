namespace ECommerce.Application.DTOs.Products;

public class SemanticSearchResponse
{
    public IReadOnlyList<ProductSummaryResponse> Items { get; set; } = Array.Empty<ProductSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string InterpretedQuery { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
}
