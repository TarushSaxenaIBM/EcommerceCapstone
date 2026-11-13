using ECommerce.AI;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Clients;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Persistence;
using ECommerce.OrderService.BackgroundJobs;
using ECommerce.OrderService.Notifications;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("order-service");
builder.Services.AddOrderPersistence(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddFoundryAi(builder.Configuration);
builder.Services.AddSignalR();

builder.Services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>((services, client) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Services:ProductServiceUrl"] ?? "http://localhost:5101/";

    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
    client.Timeout = TimeSpan.FromSeconds(30);

    var internalKey = services.GetRequiredService<InternalApiKey>();
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        InternalApiKeyMiddleware.HeaderName,
        internalKey.Value);
});

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddSingleton<IPaymentProvider, MockPaymentProvider>();
builder.Services.AddHostedService<OrderProcessingWorker>();

var app = builder.Build();

await app.Services.InitializeOrderDatabaseAsync(app.Configuration, app.Logger);

app.UseServiceDefaults();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();

public sealed class OrderServiceMarker;
