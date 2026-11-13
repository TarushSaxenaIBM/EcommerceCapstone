using ECommerce.Application.Knowledge;

namespace ECommerce.UnitTests;

public class StoreKnowledgeTests
{
    [Fact]
    public void Search_ShippingQuestion_ReturnsShippingFactFirst()
    {
        var facts = StoreKnowledge.Search("What is your shipping policy?");

        Assert.NotEmpty(facts);
        Assert.Equal("Shipping cost", facts[0].Topic);
        Assert.Contains("free", facts[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Search_TitleMatch_OutranksBodyMatch()
    {
        var facts = StoreKnowledge.Search("Do you offer a warranty on electronics?");

        Assert.Equal("Warranty", facts[0].Topic);
    }

    [Fact]
    public void Search_UnrelatedQuestion_ReturnsNothing()
    {
        var facts = StoreKnowledge.Search("quantum chromodynamics");

        Assert.Empty(facts);
    }
}
