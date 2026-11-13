using ECommerce.Application.Interfaces;

namespace ECommerce.AI;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddFoundryAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FoundryOptions>(configuration.GetSection(FoundryOptions.SectionName));
        services.AddHttpClient<IAiChatClient, FoundryChatClient>();

        return services;
    }
}
