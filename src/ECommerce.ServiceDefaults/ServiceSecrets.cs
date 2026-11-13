using System.Security.Cryptography;

namespace ECommerce.ServiceDefaults;

public static class ServiceSecrets
{
    private static readonly string DevSecretDirectory =
        Path.Combine(Path.GetTempPath(), "marketlane-dev");

    public static string ResolveJwtSecret(IConfiguration configuration, bool isDevelopment)
    {
        var configured = configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (configured.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");
            }

            return configured;
        }

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set the Jwt__Secret environment variable or store it in Key Vault.");
        }

        return GetOrCreateDevSecret("jwt-secret.dev");
    }

    public static string ResolveInternalApiKey(IConfiguration configuration, bool isDevelopment)
    {
        var configured = configuration["Internal:ApiKey"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "Internal:ApiKey is not configured. Set the Internal__ApiKey environment variable.");
        }

        return GetOrCreateDevSecret("internal-api-key.dev");
    }

    private static string GetOrCreateDevSecret(string fileName)
    {
        Directory.CreateDirectory(DevSecretDirectory);
        var path = Path.Combine(DevSecretDirectory, fileName);

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();

            if (existing.Length >= 32)
            {
                return existing;
            }
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(path, secret);

        return secret;
    }
}
