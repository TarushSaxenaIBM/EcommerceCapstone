namespace ECommerce.Application.DTOs.Products;

public class ProductQuery
{
    public string? Search { get; set; }
    public IReadOnlyList<string>? Terms { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public bool IncludeInactive { get; set; }
}
