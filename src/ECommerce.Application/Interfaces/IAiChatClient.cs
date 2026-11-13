namespace ECommerce.Application.Interfaces;

/// <summary>
/// Optional AI text completion. Implementations must return <c>null</c> when the
/// provider is not configured or unavailable so callers can fall back.
/// </summary>
public interface IAiChatClient
{
    bool IsConfigured { get; }

    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
