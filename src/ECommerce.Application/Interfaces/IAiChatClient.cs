namespace ECommerce.Application.Interfaces;

public interface IAiChatClient
{
    bool IsConfigured { get; }

    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
