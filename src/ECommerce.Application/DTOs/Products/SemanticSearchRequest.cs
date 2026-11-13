using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Products;

public class SemanticSearchRequest
{
    [Required, StringLength(300, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}
