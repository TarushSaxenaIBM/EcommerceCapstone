using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence;

internal static class ModelConfiguration
{
    public static PropertyBuilder<decimal> ConfigureMoney(
        this PropertyBuilder<decimal> property,
        DbContext context)
    {
        return context.Database.IsSqlite()
            ? property.HasConversion<double>()
            : property.HasPrecision(18, 2);
    }
}
