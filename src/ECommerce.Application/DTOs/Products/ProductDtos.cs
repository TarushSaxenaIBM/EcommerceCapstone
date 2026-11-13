using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Products;

public class CreateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Url, StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(0, 100000)]
    public int InitialStock { get; set; }
}

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

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductSuggestionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class SemanticSearchRequest
{
    [Required, StringLength(300, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}

public class SemanticSearchResponse
{
    public IReadOnlyList<ProductSummaryResponse> Items { get; set; } = Array.Empty<ProductSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string InterpretedQuery { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
}

public class UpdateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Url, StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}
