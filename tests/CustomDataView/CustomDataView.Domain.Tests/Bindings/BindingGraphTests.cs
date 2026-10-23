using CustomDataView.Domain.Services;

namespace CustomDataView.Domain.Tests.Bindings;

public class BindingGraphTests
{
    [Test(Description = "scenario_TEMP-033 - #4")]
    public void Given_ChainAndIndependentNodes_When_Ordered_Then_IndependentNodesShareWaveAndChainFollowsDependencies()
    {
        var order = BindingGraph.Order(["orders", "customer", "rate", "lines"], [("customer", "orders"), ("orders", "lines")]);

        Assert.That(order.Waves, Is.EqualTo(new[] { new[] { "customer", "rate" }, new[] { "orders" }, new[] { "lines" } }));
        Assert.That(order.Cycle, Is.Empty);
    }

    [Test(Description = "scenario_TEMP-033 - #4")]
    public void Given_NodeFedByTwoNodes_When_Ordered_Then_ItRunsAfterBoth()
    {
        var order = BindingGraph.Order(["a", "b", "c"], [("a", "c"), ("b", "c"), ("a", "c")]);

        Assert.That(order.Waves, Is.EqualTo(new[] { new[] { "a", "b" }, new[] { "c" } }));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_CycleBehindIndependentNode_When_Ordered_Then_CycleNodesAreReported()
    {
        var order = BindingGraph.Order(["a", "b", "c", "d"], [("a", "b"), ("b", "c"), ("c", "b"), ("c", "d")]);

        Assert.That(order.Waves, Is.EqualTo(new[] { new[] { "a" } }));
        Assert.That(order.Cycle, Is.EquivalentTo(new[] { "b", "c", "d" }));
    }
}
