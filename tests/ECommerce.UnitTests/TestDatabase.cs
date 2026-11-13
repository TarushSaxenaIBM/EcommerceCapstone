using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Orders;
using ECommerce.Infrastructure.Persistence.Products;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Persistence.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests;

/// <summary>
/// Creates one in-memory SQLite database per service context so tests exercise the
/// same ownership boundaries as the running microservices.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _productConnection;
    private readonly SqliteConnection _userConnection;
    private readonly SqliteConnection _orderConnection;

    private TestDatabase(
        SqliteConnection productConnection,
        SqliteConnection userConnection,
        SqliteConnection orderConnection,
        ProductDbContext productContext,
        UserDbContext userContext,
        OrderDbContext orderContext)
    {
        _productConnection = productConnection;
        _userConnection = userConnection;
        _orderConnection = orderConnection;
        ProductContext = productContext;
        UserContext = userContext;
        OrderContext = orderContext;

        ProductUnitOfWork = new UnitOfWork<ProductDbContext>(productContext);
        UserUnitOfWork = new UnitOfWork<UserDbContext>(userContext);
        OrderUnitOfWork = new UnitOfWork<OrderDbContext>(orderContext);

        Products = new ProductRepository(productContext);
        Categories = new CategoryRepository(productContext);
        Inventory = new InventoryRepository(productContext);
        Users = new UserRepository(userContext);
        Carts = new CartRepository(orderContext);
        Addresses = new AddressRepository(orderContext);
        Orders = new OrderRepository(orderContext);
        Payments = new PaymentRepository(orderContext);
    }

    public ProductDbContext ProductContext { get; }
    public UserDbContext UserContext { get; }
    public OrderDbContext OrderContext { get; }

    public IUnitOfWork ProductUnitOfWork { get; }
    public IUnitOfWork UserUnitOfWork { get; }
    public IUnitOfWork OrderUnitOfWork { get; }

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IInventoryRepository Inventory { get; }
    public IUserRepository Users { get; }
    public ICartRepository Carts { get; }
    public IAddressRepository Addresses { get; }
    public IOrderRepository Orders { get; }
    public IPaymentRepository Payments { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var productConnection = await OpenConnectionAsync();
        var userConnection = await OpenConnectionAsync();
        var orderConnection = await OpenConnectionAsync();

        var productContext = new ProductDbContext(
            new DbContextOptionsBuilder<ProductDbContext>().UseSqlite(productConnection).Options);
        var userContext = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>().UseSqlite(userConnection).Options);
        var orderContext = new OrderDbContext(
            new DbContextOptionsBuilder<OrderDbContext>().UseSqlite(orderConnection).Options);

        await productContext.Database.EnsureCreatedAsync();
        await userContext.Database.EnsureCreatedAsync();
        await orderContext.Database.EnsureCreatedAsync();

        return new TestDatabase(
            productConnection,
            userConnection,
            orderConnection,
            productContext,
            userContext,
            orderContext);
    }

    public async Task<Category> AddCategoryAsync(string? name = null)
    {
        var category = new Category { Name = name ?? $"Category {Guid.NewGuid():N}" };

        ProductContext.Categories.Add(category);
        await ProductContext.SaveChangesAsync();

        return category;
    }

    public async Task<(User User, Product Product)> SeedUserAndProductAsync(
        int stock = 10,
        decimal price = 12.50m)
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "Shopper",
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "not-used-in-these-tests"
        };

        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };

        var product = new Product
        {
            Name = "Test Product",
            Description = "A product used by the test suite.",
            Price = price,
            Sku = $"SKU-{Guid.NewGuid():N}",
            Category = category,
            Inventory = new Inventory { Quantity = stock }
        };

        UserContext.Users.Add(user);
        ProductContext.Products.Add(product);

        await UserContext.SaveChangesAsync();
        await ProductContext.SaveChangesAsync();

        return (user, product);
    }

    public async Task<Order> SeedOrderAsync(
        User user,
        Product product,
        OrderStatus status = OrderStatus.Confirmed,
        int quantity = 1)
    {
        var address = new Address
        {
            UserId = user.Id,
            FullName = "Test Shopper",
            AddressLine = "1 Test Street",
            City = "Testville",
            State = "TS",
            PostalCode = "12345",
            Country = "USA",
            PhoneNumber = "5551234567"
        };

        var order = new Order
        {
            UserId = user.Id,
            CustomerName = $"{user.FirstName} {user.LastName}",
            CustomerEmail = user.Email,
            Status = status,
            TotalAmount = product.Price * quantity,
            ShippingAddress = address,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * quantity
                }
            },
            Payment = new Payment
            {
                Amount = product.Price * quantity,
                Status = PaymentStatus.Successful,
                TransactionId = $"TEST-{Guid.NewGuid():N}"
            }
        };

        OrderContext.Orders.Add(order);
        await OrderContext.SaveChangesAsync();

        return order;
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    public async ValueTask DisposeAsync()
    {
        await ProductContext.DisposeAsync();
        await UserContext.DisposeAsync();
        await OrderContext.DisposeAsync();
        await _productConnection.DisposeAsync();
        await _userConnection.DisposeAsync();
        await _orderConnection.DisposeAsync();
    }
}
