namespace ECommerce.AI;

public class FoundryOptions
{
    public const string SectionName = "Foundry";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChatDeployment { get; set; } = string.Empty;

    public string Mode { get; set; } = "auto";

    public string ApiVersion { get; set; } = "2024-10-21";

    public string ChatPath { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 20;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
