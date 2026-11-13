using System.ComponentModel.DataAnnotations;
using ECommerce.Application.DTOs.Recommendations;

namespace ECommerce.Application.DTOs.Chat;

public class ChatMessageRequest
{
    [Required, StringLength(20)]
    public string Role { get; set; } = "user";

    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    [Required, StringLength(1500, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(10)]
    public IReadOnlyList<ChatMessageRequest> History { get; set; } = Array.Empty<ChatMessageRequest>();
}

public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public bool UsedAi { get; set; }
    public IReadOnlyList<RecommendedProductResponse> Products { get; set; } = Array.Empty<RecommendedProductResponse>();
}
