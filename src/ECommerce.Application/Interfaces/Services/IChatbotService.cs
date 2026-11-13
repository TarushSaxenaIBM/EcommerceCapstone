using ECommerce.Application.DTOs.Chat;

namespace ECommerce.Application.Interfaces.Services;

public interface IChatbotService
{
    Task<ChatResponse> AskAsync(int? userId, ChatRequest request, CancellationToken ct = default);
}
