using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Creates an administrator when Seed:AdminEmail and Seed:AdminPassword are
/// configured. No default credentials are ever committed.
/// </summary>
public static class UserSeeder
{
    public static async Task SeedAsync(
        UserDbContext db,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken ct = default)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == normalized, ct))
        {
            return;
        }

        await db.Users.AddAsync(
            new User
            {
                FirstName = "Store",
                LastName = "Admin",
                Email = normalized,
                PasswordHash = passwordHasher.Hash(password),
                Role = UserRole.Admin
            },
            ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded administrator account {Email}.", normalized);
    }
}
