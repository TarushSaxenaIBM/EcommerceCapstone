using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>Seeds a realistic starter catalog. Only runs when the catalog is empty.</summary>
public static class ProductSeeder
{
    public static async Task SeedAsync(ProductDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        var electronics = new Category
        {
            Name = "Electronics",
            Description = "Headphones, speakers and everyday technology."
        };
        var books = new Category
        {
            Name = "Books",
            Description = "Fiction and practical guides for slow weekends."
        };
        var home = new Category
        {
            Name = "Home & Kitchen",
            Description = "Simple, durable pieces for the kitchen and living room."
        };
        var sports = new Category
        {
            Name = "Sports & Outdoors",
            Description = "Gear for walking, running and everyday training."
        };

        await db.Categories.AddRangeAsync(new[] { electronics, books, home, sports }, ct);

        var products = new[]
        {
            Create(
                "Aurora Wireless Headphones",
                "Wireless over-ear headphones with good battery life, soft ear cushions and clear calls. " +
                "Up to 30 hours of playback for commutes and focused work.",
                129.99m,
                "ELEC-AUR-001",
                electronics,
                24,
                "aurora-headphones"),
            Create(
                "Nimbus Bluetooth Speaker",
                "Compact wireless speaker with a warm sound, splash resistance and 12 hours of battery. " +
                "A good desk companion or travel speaker.",
                59.99m,
                "ELEC-NIM-002",
                electronics,
                40,
                "nimbus-speaker"),
            Create(
                "The Quiet Path",
                "A calm novel about a long walk through the countryside. A thoughtful gift for readers " +
                "who enjoy character-driven fiction.",
                18.50m,
                "BOOK-QUI-003",
                books,
                60,
                "quiet-path"),
            Create(
                "Kitchen Basics Cookbook",
                "One hundred straightforward recipes for weeknight cooking, with practical notes on " +
                "pantry staples and simple meal planning.",
                24.00m,
                "BOOK-KIT-004",
                books,
                35,
                "kitchen-basics"),
            Create(
                "Cedar Serving Board",
                "A sturdy cedar board for bread, cheese and everyday prep. Finished with food-safe oil " +
                "and sized for a small kitchen.",
                42.00m,
                "HOME-CED-005",
                home,
                18,
                "cedar-board"),
            Create(
                "Linen Throw Blanket",
                "A breathable washed-linen throw for the sofa or the end of the bed. Soft from the first " +
                "use and easy to machine wash.",
                74.00m,
                "HOME-LIN-006",
                home,
                15,
                "linen-blanket"),
            Create(
                "Trailhead Running Shoes",
                "Comfortable running shoes for daily walking and light training. Lightweight cushioning, " +
                "a grippy outsole and a breathable upper for warm days.",
                89.95m,
                "SPRT-TRA-007",
                sports,
                30,
                "trailhead-shoes"),
            Create(
                "Everyday Yoga Mat",
                "A non-slip yoga mat with comfortable cushioning for stretching, mobility and home " +
                "workouts. Easy to roll up and carry.",
                34.50m,
                "SPRT-YOG-008",
                sports,
                22,
                "yoga-mat")
        };

        await db.Products.AddRangeAsync(products, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded {CategoryCount} categories and {ProductCount} products.",
            await db.Categories.CountAsync(ct),
            await db.Products.CountAsync(ct));
    }

    private static Product Create(
        string name,
        string description,
        decimal price,
        string sku,
        Category category,
        int stock,
        string imageSeed) => new()
    {
        Name = name,
        Description = description,
        Price = price,
        Sku = sku,
        Category = category,
        ImageUrl = $"https://picsum.photos/seed/{imageSeed}/600/400",
        Inventory = new Inventory
        {
            Quantity = stock,
            UpdatedAt = DateTime.UtcNow
        }
    };
}
