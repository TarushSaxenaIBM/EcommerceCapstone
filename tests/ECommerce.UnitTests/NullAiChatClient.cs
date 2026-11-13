using ECommerce.Application.Interfaces;

namespace ECommerce.UnitTests;

/// <summary>Reports no AI provider so services always use their deterministic path.</summary>
internal sealed class NullAiChatClient : IAiChatClient
{
    public bool IsConfigured => false;

    public Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
