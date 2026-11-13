namespace ECommerce.Application.Common;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ECommerce";
    public string Audience { get; set; } = "ECommerce.Client";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
