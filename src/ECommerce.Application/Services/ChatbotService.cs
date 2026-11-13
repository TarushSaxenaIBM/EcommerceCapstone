using System.Text;
using System.Text.RegularExpressions;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Chat;
using ECommerce.Application.DTOs.Recommendations;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class ChatbotService : IChatbotService
{
    private const string ShippingAnswer =
        "Shipping is free on every order in this demo store. Orders are usually delivered in 3-5 business days after they move to Processing.";

    private const string ReturnsAnswer =
        "You can return most items within 30 days of delivery. Refunds are issued to the original payment method after the returned item is checked in. This demo uses simulated payments, so no real money moves.";

    private static readonly Regex OrderIdPattern = new(
        @"\b(?:order\s*#?|#)\s*(\d{1,9})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IOrderRepository _orders;
    private readonly IProductCatalogClient _catalog;
    private readonly IAiChatClient _ai;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IOrderRepository orders,
        IProductCatalogClient catalog,
        IAiChatClient ai,
        ILogger<ChatbotService> logger)
    {
        _orders = orders;
        _catalog = catalog;
        _ai = ai;
        _logger = logger;
    }

    public async Task<ChatResponse> AskAsync(int? userId, ChatRequest request, CancellationToken ct = default)
    {
        var message = request.Message.Trim();

        if (message.Length == 0)
        {
            throw new BadRequestException("Enter a question for the support assistant.");
        }

        var lower = message.ToLowerInvariant();
        var products = await LoadProductsAsync(ct);
        var matches = MatchProducts(message, products);
        var orderFacts = await LoadOrderFactsAsync(userId, message, ct);

        if (_ai.IsConfigured)
        {
            var reply = await AskAiAsync(message, request.History, orderFacts, matches, ct);

            if (!string.IsNullOrWhiteSpace(reply))
            {
                return new ChatResponse
                {
                    Reply = reply.Trim(),
                    UsedAi = true,
                    Products = matches.Take(3).Select(ToProduct).ToList()
                };
            }
        }

        var fallback = BuildFallbackReply(lower, userId, orderFacts, matches);

        return new ChatResponse
        {
            Reply = fallback,
            UsedAi = false,
            Products = matches.Take(3).Select(ToProduct).ToList()
        };
    }

    private async Task<string?> AskAiAsync(
        string message,
        IReadOnlyList<ChatMessageRequest> history,
        string orderFacts,
        IReadOnlyList<ProductSnapshot> matches,
        CancellationToken ct)
    {
        const string systemPrompt =
            "You are the IBM ecommerce customer support assistant for an online store. " +
            "Answer only about shipping, delivery, returns, refunds, product information, order status and general store FAQs. " +
            "Use only the FACTS supplied by the application. Never invent order statuses, prices, stock levels or order details. " +
            "If the facts do not contain the answer, say you do not have that detail and suggest contacting support. " +
            "Never reveal another customer's information. Keep replies friendly and under 120 words.";

        var facts = new StringBuilder();
        facts.AppendLine("STORE FAQS:");
        facts.AppendLine("- Shipping: free, 3-5 business days after processing.");
        facts.AppendLine("- Returns: 30 days, refund after the item is checked in.");
        facts.AppendLine("- Payments: simulated in this demo; no card is charged.");
        facts.AppendLine(orderFacts);

        if (matches.Count > 0)
        {
            facts.AppendLine("MATCHING PRODUCTS:");
            foreach (var product in matches.Take(5))
            {
                facts.AppendLine(
                    $"- {product.Name} ({product.CategoryName}), ₹{product.Price:0.00}, {product.StockQuantity} in stock. {product.Description}");
            }
        }

        if (history.Count > 0)
        {
            facts.AppendLine("RECENT CONVERSATION:");
            foreach (var item in history.TakeLast(4))
            {
                var content = item.Content.Length > 300 ? item.Content[..300] : item.Content;
                facts.AppendLine($"- {item.Role}: {content}");
            }
        }

        facts.AppendLine($"CUSTOMER QUESTION: {message}");

        try
        {
            return await _ai.CompleteAsync(systemPrompt, facts.ToString(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI chatbot request failed; using FAQ fallback.");
            return null;
        }
    }

    private async Task<IReadOnlyList<ProductSnapshot>> LoadProductsAsync(CancellationToken ct)
    {
        try
        {
            return await _catalog.GetFeaturedAsync(60, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load products for the chatbot.");
            return Array.Empty<ProductSnapshot>();
        }
    }

    private async Task<string> LoadOrderFactsAsync(int? userId, string message, CancellationToken ct)
    {
        if (!userId.HasValue)
        {
            return "ORDERS: The customer is not signed in. Do not share order details; ask them to sign in.";
        }

        var orders = await _orders.GetByUserIdAsync(userId.Value, ct);

        if (orders.Count == 0)
        {
            return "ORDERS: The signed-in customer has no orders.";
        }

        var builder = new StringBuilder("ORDERS FOR THE SIGNED-IN CUSTOMER:\n");

        var mentioned = OrderIdPattern.Match(message);

        if (mentioned.Success && int.TryParse(mentioned.Groups[1].Value, out var orderId))
        {
            var order = orders.FirstOrDefault(o => o.Id == orderId);

            if (order is null)
            {
                builder.AppendLine($"- Order #{orderId} is not on this customer's account. Do not invent details.");
            }
            else
            {
                builder.AppendLine(
                    $"- Order #{order.Id}: {order.Status}, total ₹{order.TotalAmount:0.00}, placed {order.CreatedAt:yyyy-MM-dd}, items: {string.Join(", ", order.Items.Select(i => $"{i.Quantity} x {i.ProductName}"))}.");
            }

            return builder.ToString();
        }

        foreach (var order in orders.OrderByDescending(o => o.CreatedAt).Take(5))
        {
            builder.AppendLine(
                $"- Order #{order.Id}: {order.Status}, total ₹{order.TotalAmount:0.00}, placed {order.CreatedAt:yyyy-MM-dd}.");
        }

        return builder.ToString();
    }

    private static IReadOnlyList<ProductSnapshot> MatchProducts(string message, IReadOnlyList<ProductSnapshot> products)
    {
        if (products.Count == 0)
        {
            return Array.Empty<ProductSnapshot>();
        }

        var words = message.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3)
            .Distinct()
            .ToList();

        return products
            .Select(product => new
            {
                Product = product,
                Score = words.Count(word =>
                    product.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    product.Description.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                    product.CategoryName.Contains(word, StringComparison.OrdinalIgnoreCase))
            })
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Product.Price)
            .Select(entry => entry.Product)
            .Take(5)
            .ToList();
    }

    private static string BuildFallbackReply(
        string lower,
        int? userId,
        string orderFacts,
        IReadOnlyList<ProductSnapshot> matches)
    {
        if (ContainsAny(lower, "shipping", "delivery", "deliver", "how long"))
        {
            return ShippingAnswer;
        }

        if (ContainsAny(lower, "return", "refund", "money back"))
        {
            return ReturnsAnswer;
        }

        if (ContainsAny(lower, "order", "status", "track", "where is", "arrive"))
        {
            if (!userId.HasValue)
            {
                return "Please sign in and I can check the status of your own orders. I can only share order details with the account that placed the order.";
            }

            var lines = orderFacts
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("- Order #"))
                .ToList();

            return lines.Count == 0
                ? "I could not find that order on your account. Check the order number in your Orders page, or ask me about another order."
                : "Here is what I can see for your account:\n" + string.Join("\n", lines);
        }

        if (matches.Count > 0)
        {
            var lines = matches.Take(3).Select(product =>
                $"{product.Name} costs ₹{product.Price:0.00} and has {product.StockQuantity} unit(s) in stock.");

            return string.Join(" ", lines);
        }

        if (ContainsAny(lower, "hello", "hi", "hey"))
        {
            return "Hello! I can help with shipping, returns, refunds, product information and your own order status. What would you like to know?";
        }

        return "I can help with shipping, returns, refunds, product information and the status of your own orders. Try asking something like \"How long does delivery take?\" or \"Where is my order?\"";
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static RecommendedProductResponse ToProduct(ProductSnapshot product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        CategoryName = product.CategoryName,
        StockQuantity = product.StockQuantity,
        Reason = "Related to your question"
    };
}
