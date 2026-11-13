namespace ECommerce.Application.Common;

public record JwtToken(string Value, DateTime ExpiresAt);
