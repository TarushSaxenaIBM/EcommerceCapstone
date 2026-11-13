using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Clients;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Forwarder;

namespace ECommerce.IntegrationTests;

internal static class TestConfig
{
    public const string JwtSecret =
        "integration-tests-only-secret-value-that-is-long-enough-1234567890";
    public const string JwtIssuer = "ECommerce.IntegrationTests";
    public const string JwtAudience = "ECommerce.IntegrationTests.Client";
    public const string InternalApiKey =
        "integration-tests-internal-api-key-that-is-long-enough";

    public static Dictionary<string, string?> BaseSettings(string connectionName, string databasePath) =>
        new()
        {
            [$"ConnectionStrings:{connectionName}"] = $"Data Source={databasePath}",
            ["Database:Provider"] = "Sqlite",
            ["Database:ApplyMigrations"] = "false",
            ["Jwt:Secret"] = JwtSecret,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Jwt:ExpiryMinutes"] = "60",
            ["Internal:ApiKey"] = InternalApiKey,
            ["Foundry:Endpoint"] = "",
            ["Foundry:ApiKey"] = "",
            ["Foundry:ChatDeployment"] = "",
            ["Foundry:Mode"] = "auto",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
        };
}

/// <summary>
/// Base class for the in-process service hosts. Every host gets its own temporary
/// SQLite file and the same JWT/internal-key configuration so tokens minted by the
/// User Service are accepted by the other services.
/// </summary>
public abstract class ServiceApiFactory<TMarker> : WebApplicationFactory<TMarker>
    where TMarker : class
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        "marketlane-integration-tests",
        $"{typeof(TMarker).Name}-{Guid.NewGuid():N}.db");

    protected ServiceApiFactory(string connectionName)
    {
        ConnectionName = connectionName;
    }

    protected string ConnectionName { get; }

    protected string DatabasePath => _databasePath;

    protected virtual IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
        Array.Empty<KeyValuePair<string, string?>>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = TestConfig.BaseSettings(ConnectionName, _databasePath);

            foreach (var pair in ExtraSettings)
            {
                settings[pair.Key] = pair.Value;
            }

            configuration.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // A file left behind by a failed test is harmless.
        }
    }
}

public sealed class ProductApiFactory : ServiceApiFactory<global::ProductServiceMarker>
{
    public ProductApiFactory()
        : base("ProductDb")
    {
    }

    /// <summary>
    /// Handler used by the Order Service's typed HTTP client so cross-service calls
    /// travel through the real Product Service pipeline without a network socket.
    /// </summary>
    public HttpClient CreateInternalClient()
    {
        var client = new HttpClient(Server.CreateHandler(), disposeHandler: false)
        {
            BaseAddress = new Uri("http://product-service/")
        };

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            InternalApiKeyMiddleware.HeaderName,
            TestConfig.InternalApiKey);

        return client;
    }
}

public sealed class UserApiFactory : ServiceApiFactory<global::UserServiceMarker>
{
    public UserApiFactory()
        : base("UserDb")
    {
    }

    public string AdminEmail { get; } = $"admin-{Guid.NewGuid():N}@example.com";

    public string AdminPassword { get; } = "Integration-Admin-1!";

    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
    [
        new("Seed:AdminEmail", AdminEmail),
        new("Seed:AdminPassword", AdminPassword)
    ];
}

public sealed class OrderApiFactory : ServiceApiFactory<global::OrderServiceMarker>
{
    private readonly ProductApiFactory _products;

    public OrderApiFactory(ProductApiFactory products)
        : base("OrderDb")
    {
        _products = products;
    }

    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
    [
        new("Orders:AutoProcessAfterMinutes", "0"),
        new("Orders:AutoProcessIntervalSeconds", "3600")
    ];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductCatalogClient>();
            services.AddScoped<IProductCatalogClient>(provider =>
                new ProductCatalogClient(
                    _products.CreateInternalClient(),
                    provider.GetRequiredService<ILogger<ProductCatalogClient>>()));
        });
    }
}

/// <summary>
/// Routes gateway proxy requests to the matching in-process TestServer based on the
/// configured destination authority.
/// </summary>
internal sealed class TestServerRoutingHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpMessageInvoker> _invokers;

    public TestServerRoutingHandler(IReadOnlyDictionary<string, HttpMessageHandler> handlers)
    {
        _invokers = handlers.ToDictionary(
            pair => pair.Key,
            pair => new HttpMessageInvoker(pair.Value, disposeHandler: false),
            StringComparer.OrdinalIgnoreCase);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authority = request.RequestUri?.Authority;

        if (authority is null || !_invokers.TryGetValue(authority, out var invoker))
        {
            throw new HttpRequestException(
                $"No in-process service is registered for '{authority}'.");
        }

        return invoker.SendAsync(request, cancellationToken);
    }
}

internal sealed class TestForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly HttpMessageInvoker _invoker;

    public TestForwarderHttpClientFactory(
        ProductApiFactory products,
        UserApiFactory users,
        OrderApiFactory orders)
    {
        var handler = new TestServerRoutingHandler(new Dictionary<string, HttpMessageHandler>
        {
            ["localhost:5101"] = products.Server.CreateHandler(),
            ["localhost:5102"] = users.Server.CreateHandler(),
            ["localhost:5103"] = orders.Server.CreateHandler()
        });

        _invoker = new HttpMessageInvoker(handler, disposeHandler: false);
    }

    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => _invoker;
}

public sealed class GatewayApiFactory : WebApplicationFactory<global::GatewayMarker>
{
    private readonly ProductApiFactory _products;
    private readonly UserApiFactory _users;
    private readonly OrderApiFactory _orders;

    public GatewayApiFactory(
        ProductApiFactory products,
        UserApiFactory users,
        OrderApiFactory orders)
    {
        _products = products;
        _users = users;
        _orders = orders;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:ProductServiceUrl"] = "http://localhost:5101",
                ["Services:UserServiceUrl"] = "http://localhost:5102",
                ["Services:OrderServiceUrl"] = "http://localhost:5103",
                ["Jwt:Secret"] = TestConfig.JwtSecret,
                ["Jwt:Issuer"] = TestConfig.JwtIssuer,
                ["Jwt:Audience"] = TestConfig.JwtAudience,
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IForwarderHttpClientFactory>();
            services.AddSingleton<IForwarderHttpClientFactory>(
                new TestForwarderHttpClientFactory(_products, _users, _orders));
        });
    }
}

/// <summary>
/// Gateway pointed at a port that is guaranteed to be closed. Used to verify the
/// proxy failure contract without starting an upstream service.
/// </summary>
public sealed class UnreachableGatewayFactory : WebApplicationFactory<global::GatewayMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:ProductServiceUrl"] = "http://127.0.0.1:1",
                ["Services:UserServiceUrl"] = "http://127.0.0.1:1",
                ["Services:OrderServiceUrl"] = "http://127.0.0.1:1",
                ["Jwt:Secret"] = TestConfig.JwtSecret,
                ["Jwt:Issuer"] = TestConfig.JwtIssuer,
                ["Jwt:Audience"] = TestConfig.JwtAudience,
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });
        });
    }
}

/// <summary>
/// Shared set of hosts for the end-to-end storefront tests. Product, User and Order
/// services run in-process; the gateway proxies to them through their TestServer
/// handlers, so routing, JWT validation and cross-service HTTP calls are exercised.
/// </summary>
public sealed class StoreFixture : IDisposable
{
    private static readonly (string Name, string Value)[] Secrets =
    [
        ("Jwt__Secret", TestConfig.JwtSecret),
        ("Jwt__Issuer", TestConfig.JwtIssuer),
        ("Jwt__Audience", TestConfig.JwtAudience),
        ("Internal__ApiKey", TestConfig.InternalApiKey)
    ];

    public StoreFixture()
    {
        // ServiceSecrets reads these values while the host is being configured, which
        // is earlier than the in-memory configuration added by WebApplicationFactory.
        foreach (var (name, value) in Secrets)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        Products = new ProductApiFactory();
        Users = new UserApiFactory();
        Orders = new OrderApiFactory(Products);
        Gateway = new GatewayApiFactory(Products, Users, Orders);
    }

    public ProductApiFactory Products { get; }
    public UserApiFactory Users { get; }
    public OrderApiFactory Orders { get; }
    public GatewayApiFactory Gateway { get; }

    public string AdminEmail => Users.AdminEmail;
    public string AdminPassword => Users.AdminPassword;

    public void Dispose()
    {
        Gateway.Dispose();
        Orders.Dispose();
        Users.Dispose();
        Products.Dispose();

        foreach (var (name, _) in Secrets)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
