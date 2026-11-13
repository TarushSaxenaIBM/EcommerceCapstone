using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.DTOs.Chat;
using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Recommendations;
using ECommerce.Application.DTOs.Users;
using ECommerce.Domain.Enums;

namespace ECommerce.IntegrationTests;

public class StoreApiTests : IClassFixture<StoreFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly StoreFixture _fixture;

    public StoreApiTests(StoreFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Catalog_ReturnsSeededProductsAndCategoriesThroughGateway()
    {
        var client = GatewayClient();

        var products = await client.GetFromJsonAsync<PagedResult<ProductSummaryResponse>>(
            "/api/products",
            JsonOptions);
        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>(
            "/api/categories",
            JsonOptions);

        Assert.NotNull(products);
        Assert.True(products.TotalCount >= 8);
        Assert.All(products.Items, product => Assert.True(product.IsActive));
        Assert.NotNull(categories);
        Assert.True(categories.Count >= 4);
    }

    [Fact]
    public async Task SearchAndSuggestions_AreAvailableWithoutAuthentication()
    {
        var client = GatewayClient();

        var searchResponse = await client.PostAsJsonAsync(
            "/api/products/search",
            new SemanticSearchRequest { Query = "lightweight running shoes" },
            JsonOptions);
        var suggestions = await client.GetFromJsonAsync<List<ProductSuggestionResponse>>(
            "/api/products/suggestions?query=nimb",
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var search = await searchResponse.Content.ReadFromJsonAsync<SemanticSearchResponse>(JsonOptions);
        Assert.NotNull(search);
        Assert.False(search.UsedAi);
        Assert.Contains(search.Items, item => item.Name == "Trailhead Running Shoes");
        Assert.NotNull(suggestions);
        Assert.Contains(suggestions, item => item.Name == "Nimbus Bluetooth Speaker");
    }

    [Fact]
    public async Task ProtectedRoutes_WithoutToken_ReturnProblemDetails401()
    {
        var client = GatewayClient();

        foreach (var path in new[] { "/api/cart", "/api/orders", "/api/users", "/api/inventory/1" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = GatewayClient();
        var request = NewRegistration();

        var first = await client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);
        var second = await client.PostAsJsonAsync("/api/auth/register", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        var client = GatewayClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Invalid",
                LastName = "User",
                Email = "not-an-email",
                Password = "short"
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = GatewayClient();
        var auth = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = auth.User.Email, Password = "wrong-password" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotCreateProducts()
    {
        var client = await CreateCustomerClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest
            {
                Name = "Not allowed",
                Description = "Customers cannot create products.",
                Price = 10,
                Sku = $"NOPE-{Guid.NewGuid():N}",
                CategoryId = 1,
                InitialStock = 1
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanManageProductsAndInventory()
    {
        var admin = await CreateAdminClientAsync();
        var categories = await admin.GetFromJsonAsync<List<CategoryResponse>>(
            "/api/categories",
            JsonOptions);
        var categoryId = categories!.First().Id;
        var sku = $"IT-{Guid.NewGuid():N}";

        var createResponse = await admin.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest
            {
                Name = "Integration Product",
                Description = "Created by the integration test suite.",
                Price = 9.99m,
                Sku = sku,
                CategoryId = categoryId,
                InitialStock = 5
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(5, created.StockQuantity);

        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/products/{created.Id}",
            new UpdateProductRequest
            {
                Name = "Updated Integration Product",
                Description = "Updated by the integration test suite.",
                Price = 14.50m,
                Sku = sku,
                CategoryId = categoryId,
                IsActive = true
            },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated Integration Product", updated.Name);
        Assert.Equal(14.50m, updated.Price);

        var setStock = await admin.PutAsJsonAsync(
            $"/api/inventory/{created.Id}",
            new UpdateInventoryRequest { Quantity = 9 },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, setStock.StatusCode);
        var stock = await setStock.Content.ReadFromJsonAsync<InventoryResponse>(JsonOptions);
        Assert.Equal(9, stock!.Quantity);

        var adjust = await admin.PostAsJsonAsync(
            $"/api/inventory/{created.Id}/adjust",
            new AdjustInventoryRequest { QuantityChange = -4 },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);
        var adjusted = await adjust.Content.ReadFromJsonAsync<InventoryResponse>(JsonOptions);
        Assert.Equal(5, adjusted!.Quantity);
    }

    [Fact]
    public async Task Checkout_CreatesOrderUpdatesStockAndClearsCart()
    {
        var client = await CreateCustomerClientAsync();
        var product = await GetFirstProductAsync(client);
        var stockBefore = (await client.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{product.Id}",
            JsonOptions))!.StockQuantity;

        var addResponse = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest { ProductId = product.Id, Quantity = 2 },
            JsonOptions);
        addResponse.EnsureSuccessStatusCode();

        var order = await CheckoutAsync(client);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(2, order.Items.Single().Quantity);
        Assert.Equal(product.Price * 2, order.TotalAmount);
        Assert.NotNull(order.Payment);
        Assert.Equal(PaymentStatus.Successful, order.Payment.Status);

        var cart = await client.GetFromJsonAsync<CartResponse>("/api/cart", JsonOptions);
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);

        var stockAfter = (await client.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{product.Id}",
            JsonOptions))!.StockQuantity;
        Assert.Equal(stockBefore - 2, stockAfter);

        var detail = await client.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{order.Id}",
            JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal("Integration Tester", detail.ShippingAddress.FullName);

        var payment = await client.GetFromJsonAsync<PaymentResponse>(
            $"/api/payments/{order.Id}",
            JsonOptions);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Successful, payment.Status);
    }

    [Fact]
    public async Task Order_FromAnotherCustomer_ReturnsNotFound()
    {
        var owner = await CreateCustomerClientAsync();
        var order = await CreateOrderAsync(owner);

        var otherCustomer = await CreateCustomerClientAsync();
        var response = await otherCustomer.GetAsync($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanMoveOrderToProcessing_ButCustomerCannot()
    {
        var customer = await CreateCustomerClientAsync();
        var order = await CreateOrderAsync(customer);

        var forbidden = await customer.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest { Status = OrderStatus.Processing },
            JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var admin = await CreateAdminClientAsync();
        var response = await admin.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest { Status = OrderStatus.Processing },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(OrderStatus.Processing, updated.Status);

        var customerView = await customer.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{order.Id}",
            JsonOptions);
        Assert.Equal(OrderStatus.Processing, customerView!.Status);
    }

    [Fact]
    public async Task Checkout_WhenStockChangesAfterCartAdd_ReturnsConflict()
    {
        var admin = await CreateAdminClientAsync();
        var categories = await admin.GetFromJsonAsync<List<CategoryResponse>>(
            "/api/categories",
            JsonOptions);

        var createResponse = await admin.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest
            {
                Name = "Limited Stock Product",
                Description = "Used to verify checkout stock validation.",
                Price = 20m,
                Sku = $"LIMITED-{Guid.NewGuid():N}",
                CategoryId = categories!.First().Id,
                InitialStock = 5
            },
            JsonOptions);

        createResponse.EnsureSuccessStatusCode();
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var customer = await CreateCustomerClientAsync();
        var addResponse = await customer.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest { ProductId = product.Id, Quantity = 2 },
            JsonOptions);
        addResponse.EnsureSuccessStatusCode();

        var setStock = await admin.PutAsJsonAsync(
            $"/api/inventory/{product.Id}",
            new UpdateInventoryRequest { Quantity = 1 },
            JsonOptions);
        setStock.EnsureSuccessStatusCode();

        var checkoutResponse = await customer.PostAsJsonAsync(
            "/api/checkout",
            CheckoutRequest(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, checkoutResponse.StatusCode);
    }

    [Fact]
    public async Task Recommendations_AreServedWithoutAuthentication()
    {
        var client = GatewayClient();
        var product = await GetFirstProductAsync(client);

        var home = await client.GetFromJsonAsync<RecommendationResponse>(
            "/api/recommendations/home",
            JsonOptions);
        var related = await client.GetFromJsonAsync<RecommendationResponse>(
            $"/api/recommendations/related/{product.Id}",
            JsonOptions);

        Assert.NotNull(home);
        Assert.NotEmpty(home.Items);
        Assert.False(home.AiRanked);
        Assert.NotNull(related);
        Assert.NotEmpty(related.Items);
        Assert.DoesNotContain(related.Items, item => item.Id == product.Id);
    }

    [Fact]
    public async Task Chat_ReturnsDeterministicFallbackWhenAiIsNotConfigured()
    {
        var client = GatewayClient();

        var response = await client.PostAsJsonAsync(
            "/api/chat",
            new ChatRequest { Message = "What is your shipping policy?" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);
        Assert.NotNull(chat);
        Assert.False(chat.UsedAi);
        Assert.Contains("free", chat.Reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customer_CanUpdateProfileAndReadItBack()
    {
        var client = await CreateCustomerClientAsync();

        var updateResponse = await client.PutAsJsonAsync(
            "/api/users/me",
            new UpdateProfileRequest { FirstName = "Updated", LastName = "Shopper" },
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/auth/me", JsonOptions);
        Assert.NotNull(me);
        Assert.Equal("Updated", me.FirstName);
        Assert.Equal("Shopper", me.LastName);
    }

    [Fact]
    public async Task Admin_CanListUsers()
    {
        var admin = await CreateAdminClientAsync();

        var users = await admin.GetFromJsonAsync<List<UserResponse>>("/api/users", JsonOptions);

        Assert.NotNull(users);
        Assert.Contains(users, user => user.Email == _fixture.AdminEmail);
    }

    private HttpClient GatewayClient() => _fixture.Gateway.CreateClient();

    private async Task<HttpClient> CreateCustomerClientAsync()
    {
        var client = GatewayClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = GatewayClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = _fixture.AdminEmail, Password = _fixture.AdminPassword },
            JsonOptions);

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(), JsonOptions);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    private static RegisterRequest NewRegistration() => new()
    {
        FirstName = "Integration",
        LastName = "Tester",
        Email = $"it-{Guid.NewGuid():N}@example.com",
        Password = "Passw0rd!"
    };

    private static async Task<ProductSummaryResponse> GetFirstProductAsync(HttpClient client)
    {
        var products = await client.GetFromJsonAsync<PagedResult<ProductSummaryResponse>>(
            "/api/products",
            JsonOptions);

        return products!.Items.First();
    }

    private static async Task<OrderResponse> CreateOrderAsync(HttpClient client)
    {
        var product = await GetFirstProductAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            "/api/cart/items",
            new AddCartItemRequest { ProductId = product.Id, Quantity = 1 },
            JsonOptions);
        addResponse.EnsureSuccessStatusCode();

        return await CheckoutAsync(client);
    }

    private static async Task<OrderResponse> CheckoutAsync(HttpClient client)
    {
        var checkoutResponse = await client.PostAsJsonAsync(
            "/api/checkout",
            CheckoutRequest(),
            JsonOptions);

        checkoutResponse.EnsureSuccessStatusCode();
        return (await checkoutResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions))!;
    }

    private static CheckoutRequest CheckoutRequest() => new()
    {
        ShippingAddress = new AddressRequest
        {
            FullName = "Integration Tester",
            AddressLine = "10 Test Lane",
            City = "Testville",
            State = "TS",
            PostalCode = "12345",
            Country = "USA",
            PhoneNumber = "5551234567"
        }
    };
}

public class GatewayFailureTests
{
    [Fact]
    public async Task UnreachableUpstream_ReturnsProblemDetails502()
    {
        using var factory = new UnreachableGatewayFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}
