namespace ECommerce.AI;

/// <summary>
/// Configuration for the Microsoft Foundry / Azure OpenAI compatible chat endpoint.
/// Values are supplied through configuration or environment variables, never code.
/// </summary>
public class FoundryOptions
{
    public const string SectionName = "Foundry";

    /// <summary>Resource endpoint, for example https://my-foundry.services.ai.azure.com.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>API key created in the Foundry resource. Prefer Key Vault in Azure.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model or deployment name, for example gpt-4o-mini.</summary>
    public string ChatDeployment { get; set; } = string.Empty;

    /// <summary>Azure OpenAI deployment path style (deployments) or Foundry models path style.</summary>
    public string Mode { get; set; } = "auto";

    /// <summary>API version used by the chat completions endpoint.</summary>
    public string ApiVersion { get; set; } = "2024-10-21";

    /// <summary>Optional explicit request path overriding the mode based path.</summary>
    public string ChatPath { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 20;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
