using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces.Repositories;

public interface IAddressRepository
{
    Task AddAsync(Address address, CancellationToken ct = default);
}

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeCategoryId = null, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    void Update(Category category);
}

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task AddAsync(Inventory inventory, CancellationToken ct = default);
    void Update(Inventory inventory);
    Task<bool> TryDecrementAsync(int productId, int quantity, CancellationToken ct = default);
    Task IncrementAsync(int productId, int quantity, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByStatusOlderThanAsync(OrderStatus status, DateTime createdBefore, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetPopularProductIdsAsync(int take, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    void Update(Order order);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task AddAsync(Payment payment, CancellationToken ct = default);
    void Update(Payment payment);
}

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
}
