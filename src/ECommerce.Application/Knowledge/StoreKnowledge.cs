namespace ECommerce.Application.Knowledge;

public sealed record StoreFact(string Topic, string Text);

// Approved store facts. This is the data the support assistant is allowed to answer from:
// policy text that the business owns, kept next to the code so it is easy to review.
public static class StoreKnowledge
{
    private static readonly StoreFact[] Facts =
    {
        new("Shipping cost",
            "Shipping is free on every order. There is no minimum order value and no separate delivery fee."),
        new("Delivery time",
            "Orders are usually delivered in 3-5 business days after they move to Processing. Remote pin codes can take up to 7 business days."),
        new("Delivery areas",
            "We deliver across India. If a pin code is not serviceable, the checkout page shows the message before payment."),
        new("Returns",
            "Most items can be returned within 30 days of delivery, as long as they are unused and in the original packaging."),
        new("Refunds",
            "Refunds are issued to the original payment method after the returned item is checked in. They usually appear within 5-7 business days."),
        new("Exchanges",
            "A damaged or wrong item can be exchanged instead of returned. Contact support with the order number and a photo of the item."),
        new("Order tracking",
            "Order status is shown on the Orders page and updated live. You can also ask the assistant to look up an order on your own account."),
        new("Cancellation",
            "An order can be cancelled while it is still Pending or Confirmed. Once it moves to Shipped it has to be returned instead."),
        new("Payments",
            "Payments in this demo are simulated. The checkout page collects an address and confirms the order, but no card is charged."),
        new("Payment methods",
            "The demo supports a simulated card payment. A production store would add UPI, net banking and cash on delivery."),
        new("Warranty",
            "Electronics carry a 12-month manufacturer warranty. Warranty claims need the order number and the product serial number."),
        new("Invoices and GST",
            "A GST invoice is generated for every order and is available from the order details page."),
        new("Support hours",
            "Support answers questions here at any time. A human agent is available Monday to Saturday, 9:00 to 18:00 IST."),
        new("Bulk orders",
            "For more than 10 units of one product, contact support before ordering so stock can be reserved."),
        new("Stock",
            "Stock is checked again at checkout. If another customer buys the last unit first, checkout stops and asks you to update the cart.")
    };

    // Returns the facts that share the most words with the question. Keyword matching rather than
    // vector search: the fact list is small, and the result is easy to explain and test.
    public static IReadOnlyList<StoreFact> Search(string question, int maxResults = 3)
    {
        var words = TextTokens.From(question);

        if (words.Count == 0)
        {
            return Array.Empty<StoreFact>();
        }

        return Facts
            .Select(fact => (Fact: fact, Score: Score(fact, words)))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .Take(maxResults)
            .Select(match => match.Fact)
            .ToList();
    }

    private static int Score(StoreFact fact, IReadOnlyCollection<string> words)
    {
        var topic = fact.Topic.ToLowerInvariant();
        var text = fact.Text.ToLowerInvariant();

        return words.Sum(word =>
            topic.Contains(word, StringComparison.Ordinal) ? 3 :
            text.Contains(word, StringComparison.Ordinal) ? 1 : 0);
    }
}
