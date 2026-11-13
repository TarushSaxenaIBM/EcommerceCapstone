using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Products;

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
