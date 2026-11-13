using System.Security.Claims;

namespace ECommerce.ServiceDefaults;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The user id claim is missing.");
    }

    public static string GetUserEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue("email")
        ?? throw new InvalidOperationException("The email claim is missing.");

    public static string GetUserName(this ClaimsPrincipal user)
    {
        var name = user.FindFirstValue(ClaimTypes.Name);

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var given = user.FindFirstValue(ClaimTypes.GivenName);
        var family = user.FindFirstValue(ClaimTypes.Surname);

        return $"{given} {family}".Trim() is { Length: > 0 } combined
            ? combined
            : "IBM ecommerce customer";
    }
}
