namespace ECommerce.Application.DTOs.Recommendations;

public class RecommendedProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RecommendationResponse
{
    public IReadOnlyList<RecommendedProductResponse> Items { get; set; } = Array.Empty<RecommendedProductResponse>();
    public bool Personalized { get; set; }
    public bool AiRanked { get; set; }
}
