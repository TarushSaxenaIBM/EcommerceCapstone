using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Chat;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Common;
using ECommerce.Domain.Enums;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Recommendations;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserResponse> GetCurrentUserAsync(int userId, CancellationToken ct = default);
}

public interface ICartService
{
    Task<CartResponse> GetCartAsync(int userId, CancellationToken ct = default);
    Task<CartResponse> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct = default);
    Task<CartResponse> UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct = default);
    Task<CartResponse> RemoveItemAsync(int userId, int itemId, CancellationToken ct = default);
    Task<CartResponse> ClearAsync(int userId, CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<CategoryResponse> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IChatbotService
{
    Task<ChatResponse> AskAsync(int? userId, ChatRequest request, CancellationToken ct = default);
}

public interface ICheckoutService
{
    Task<OrderResponse> CheckoutAsync(
        int userId,
        string customerName,
        string customerEmail,
        CheckoutRequest request,
        CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<InventoryResponse> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task<InventoryResponse> UpdateAsync(int productId, UpdateInventoryRequest request, CancellationToken ct = default);
    Task<InventoryResponse> AdjustAsync(int productId, AdjustInventoryRequest request, CancellationToken ct = default);
    Task<bool> TryReserveAsync(int productId, int quantity, CancellationToken ct = default);
    Task ReleaseAsync(int productId, int quantity, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryResponse>> GetMyOrdersAsync(int userId, CancellationToken ct = default);
    Task<OrderResponse> GetByIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default);
    Task<PagedResult<OrderSummaryResponse>> GetPagedAsync(int page, int pageSize, OrderStatus? status, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default);
}

public interface IPaymentService
{
    Task<PaymentResponse> GetByOrderIdAsync(int userId, int orderId, bool isAdmin, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedResult<ProductSummaryResponse>> GetPagedAsync(ProductQuery query, CancellationToken ct = default);
    Task<ProductResponse> GetByIdAsync(int id, bool includeInactive, CancellationToken ct = default);
    Task<IReadOnlyList<ProductResponse>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<ProductResponse>> GetFeaturedAsync(int take, CancellationToken ct = default);
    Task<SemanticSearchResponse> SearchAsync(SemanticSearchRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSuggestionResponse>> GetSuggestionsAsync(string query, int take, CancellationToken ct = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IRecommendationService
{
    Task<RecommendationResponse> GetHomeAsync(int? userId, CancellationToken ct = default);

    Task<RecommendationResponse> GetRelatedAsync(int productId, int? userId, CancellationToken ct = default);
}

public interface IUserProfileService
{
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UserResponse> UpdateRoleAsync(int userId, UpdateUserRoleRequest request, CancellationToken ct = default);
}
