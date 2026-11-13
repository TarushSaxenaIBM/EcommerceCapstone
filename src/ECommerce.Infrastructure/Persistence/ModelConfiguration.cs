using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence;

internal static class ModelConfiguration
{
    /// <summary>
    /// SQLite stores decimal values as REAL, which is required for price sorting and
    /// range filters. SQL Server uses an exact decimal column instead.
    /// </summary>
    public static PropertyBuilder<decimal> ConfigureMoney(
        this PropertyBuilder<decimal> property,
        DbContext context)
    {
        return context.Database.IsSqlite()
            ? property.HasConversion<double>()
            : property.HasPrecision(18, 2);
    }
}
