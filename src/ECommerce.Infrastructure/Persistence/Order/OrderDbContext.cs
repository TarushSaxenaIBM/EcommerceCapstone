using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Orders;

/// <summary>
/// Database owned by the Order Service: carts, orders, payments and address
/// snapshots. It intentionally contains no users or products.
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(c => c.UserId).IsUnique();
            entity.HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.ImageUrl).HasMaxLength(500);
            entity.Property(i => i.UnitPrice).ConfigureMoney(this);
            entity.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.Property(a => a.FullName).HasMaxLength(150).IsRequired();
            entity.Property(a => a.AddressLine).HasMaxLength(300).IsRequired();
            entity.Property(a => a.City).HasMaxLength(100).IsRequired();
            entity.Property(a => a.State).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Country).HasMaxLength(100).IsRequired();
            entity.Property(a => a.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.CustomerName).HasMaxLength(150).IsRequired();
            entity.Property(o => o.CustomerEmail).HasMaxLength(200).IsRequired();
            entity.Property(o => o.TotalAmount).ConfigureMoney(this);
            entity.HasIndex(o => new { o.UserId, o.CreatedAt });

            entity.HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.UnitPrice).ConfigureMoney(this);
            entity.Property(i => i.TotalPrice).ConfigureMoney(this);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).ConfigureMoney(this);
            entity.Property(p => p.TransactionId).HasMaxLength(100);
            entity.HasIndex(p => p.OrderId).IsUnique();
        });
    }
}
