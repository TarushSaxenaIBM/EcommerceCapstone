using System.Security.Cryptography;
using System.Text;

namespace ECommerce.ServiceDefaults;

public sealed class InternalApiKeyMiddleware
{
    public const string HeaderName = "X-Internal-Api-Key";
    private const string PathPrefix = "/api/internal";

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedKey;

    public InternalApiKeyMiddleware(RequestDelegate next, string expectedKey)
    {
        _next = next;
        _expectedKey = Encoding.UTF8.GetBytes(expectedKey);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(PathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var supplied = context.Request.Headers[HeaderName].ToString();

        if (!IsValid(supplied))
        {
            await ExceptionHandlingMiddleware.WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "A valid internal API key is required.");
            return;
        }

        await _next(context);
    }

    private bool IsValid(string supplied)
    {
        if (string.IsNullOrEmpty(supplied))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(supplied);

        return bytes.Length == _expectedKey.Length &&
               CryptographicOperations.FixedTimeEquals(bytes, _expectedKey);
    }
}
