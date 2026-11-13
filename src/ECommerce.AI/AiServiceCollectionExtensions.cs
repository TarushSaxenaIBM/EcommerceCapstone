using ECommerce.Application.Interfaces;

namespace ECommerce.AI;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Foundry chat client. When the endpoint and key are missing the
    /// same client reports <see cref="IAiChatClient.IsConfigured"/> as false and the
    /// application falls back to heuristic behaviour.
    /// </summary>
    public static IServiceCollection AddFoundryAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddHttpClient<IAiChatClient, FoundryChatClient>();

        return services;
    }
}
