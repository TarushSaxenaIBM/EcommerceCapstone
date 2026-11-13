using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Infrastructure.Persistence.Orders;
using ECommerce.Infrastructure.Persistence.Products;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Persistence.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Registers one isolated database per service. Provider and connection strings
/// come from configuration so local SQLite and Azure SQL share the same code.
/// </summary>
public static class PersistenceExtensions
{
    public static IServiceCollection AddProductPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProductDbContext>(options =>
            Configure(options, configuration, "ProductDb", "product.db"));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<ProductDbContext>>();

        return services;
    }

    public static IServiceCollection AddUserPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
            Configure(options, configuration, "UserDb", "user.db"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<UserDbContext>>();

        return services;
    }

    public static IServiceCollection AddOrderPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            Configure(options, configuration, "OrderDb", "order.db"));

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork<OrderDbContext>>();

        return services;
    }

    public static async Task InitializeProductDatabaseAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        await PrepareAsync(db, configuration, logger, ct);
        await ProductSeeder.SeedAsync(db, logger, ct);
    }

    public static async Task InitializeUserDatabaseAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger logger,
        CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        await PrepareAsync(db, configuration, logger, ct);
        await UserSeeder.SeedAsync(db, configuration, passwordHasher, logger, ct);
    }

    public static async Task InitializeOrderDatabaseAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        await PrepareAsync(db, configuration, logger, ct);
    }

    private static void Configure(
        DbContextOptionsBuilder options,
        IConfiguration configuration,
        string connectionName,
        string defaultFileName)
    {
        var connectionString = configuration.GetConnectionString(connectionName)
            ?? $"Data Source={Path.Combine("data", defaultFileName)}";
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlServer(connectionString);
            return;
        }

        options.UseSqlite(connectionString);
    }

    private static async Task PrepareAsync(
        DbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var applyMigrations = !bool.TryParse(configuration["Database:ApplyMigrations"], out var configured) ||
                              configured;

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // SQLite migrations are not valid T-SQL. Azure deployments use EnsureCreated
            // unless a SQL Server migration bundle is generated in the release pipeline.
            await db.Database.EnsureCreatedAsync(ct);
            logger.LogInformation("SQL Server schema ensured for {Context}.", db.GetType().Name);
            return;
        }

        EnsureSqliteDirectory(db.Database.GetConnectionString());

        if (applyMigrations)
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }
    }

    private static void EnsureSqliteDirectory(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource) ||
            builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
