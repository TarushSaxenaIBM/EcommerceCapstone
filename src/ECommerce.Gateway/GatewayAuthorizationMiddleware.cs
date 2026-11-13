using ECommerce.ServiceDefaults;

namespace ECommerce.Gateway;

/// <summary>
/// Applies the public API's authentication boundary at the edge. Each downstream
/// service still verifies the JWT and enforces its own roles and ownership rules.
/// </summary>
public sealed class GatewayAuthorizationMiddleware
{
    private static readonly PathString[] ProtectedPrefixes =
    [
        "/api/cart",
        "/api/orders",
        "/api/checkout",
        "/api/payments",
        "/api/inventory",
        "/api/auth/me",
        "/api/users"
    ];

    private readonly RequestDelegate _next;

    public GatewayAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAuthentication(context.Request))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await ExceptionHandlingMiddleware.WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication is required for this endpoint.");
            return;
        }

        await _next(context);
    }

    private static bool RequiresAuthentication(HttpRequest request)
    {
        var path = request.Path;

        if (ProtectedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HttpMethods.IsOptions(request.Method) ||
            HttpMethods.IsGet(request.Method) ||
            HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        // Product search is a read-only POST and stays anonymous. Every other write to
        // the catalog or category endpoints requires an authenticated request; the
        // downstream services enforce the Admin role.
        if (path.StartsWithSegments("/api/products/search", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        return path.StartsWithSegments("/api/products", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/categories", StringComparison.OrdinalIgnoreCase);
    }
}
