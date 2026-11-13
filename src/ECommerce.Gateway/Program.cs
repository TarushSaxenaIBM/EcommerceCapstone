using ECommerce.Gateway;
using ECommerce.ServiceDefaults;
using Microsoft.AspNetCore.Http.Features;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("gateway");

var services = builder.Configuration;
var productUrl = Normalize(services["Services:ProductServiceUrl"] ?? "http://localhost:5101");
var userUrl = Normalize(services["Services:UserServiceUrl"] ?? "http://localhost:5102");
var orderUrl = Normalize(services["Services:OrderServiceUrl"] ?? "http://localhost:5103");

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(
        BuildRoutes(),
        BuildClusters(productUrl, userUrl, orderUrl));

var app = builder.Build();

app.UseWebSockets();
app.UseServiceDefaults();
app.UseMiddleware<GatewayAuthorizationMiddleware>();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        await next();

        var error = context.Features.Get<IForwarderErrorFeature>();

        if (error is null || context.Response.HasStarted)
        {
            return;
        }

        var (status, message) = error.Error switch
        {
            ForwarderError.RequestTimedOut =>
                (StatusCodes.Status504GatewayTimeout as int?, "The downstream service did not respond in time."),
            ForwarderError.RequestCanceled =>
                (499 as int?, "The request was cancelled."),
            _ =>
                (StatusCodes.Status502BadGateway as int?, "The downstream service is unavailable.")
        };

        if (status == 499)
        {
            return;
        }

        await ExceptionHandlingMiddleware.WriteProblemAsync(context, status!.Value, message);
    });
});

app.Run();

static string Normalize(string url) => url.EndsWith('/') ? url : $"{url}/";

static IReadOnlyList<RouteConfig> BuildRoutes() =>
[
    new RouteConfig
    {
        RouteId = "products",
        ClusterId = "product-cluster",
        Match = new RouteMatch { Path = "/api/products/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "categories",
        ClusterId = "product-cluster",
        Match = new RouteMatch { Path = "/api/categories/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "inventory",
        ClusterId = "product-cluster",
        Match = new RouteMatch { Path = "/api/inventory/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "auth",
        ClusterId = "user-cluster",
        Match = new RouteMatch { Path = "/api/auth/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "users",
        ClusterId = "user-cluster",
        Match = new RouteMatch { Path = "/api/users/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "cart",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/cart/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "orders",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/orders/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "checkout",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/checkout/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "payments",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/payments/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "recommendations",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/recommendations/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "chat",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/api/chat/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "order-hub",
        ClusterId = "order-cluster",
        Match = new RouteMatch { Path = "/hubs/{**catch-all}" }
    }
];

static IReadOnlyList<ClusterConfig> BuildClusters(string productUrl, string userUrl, string orderUrl) =>
[
    new ClusterConfig
    {
        ClusterId = "product-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = productUrl }
        }
    },
    new ClusterConfig
    {
        ClusterId = "user-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = userUrl }
        }
    },
    new ClusterConfig
    {
        ClusterId = "order-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = orderUrl }
        }
    }
];

public sealed class GatewayMarker;
