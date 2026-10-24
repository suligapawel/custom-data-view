using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class TileDefinitionQueriesTests
{
    private static readonly Guid RateConnectionId = Guid.NewGuid();
    private static readonly Guid OrdersConnectionId = Guid.NewGuid();

    private static readonly CombinationService Combinations =
        new([new CoalesceCombination(), new JoinCombination(), new ObjectCombination(), new TemplateCombination()]);

    [Test(Description = "scenario_TEMP-015-023 - #18")]
    public void Given_TileWithConnectionSource_When_ContractIsRead_Then_UnboundParametersAreRequiredOnlyWithoutDefault()
    {
        var definition = new TileDefinition(
            "Kurs",
            TileDisplays.Details,
            [new TileSource("rate", TileSourceKinds.Connection, RateConnectionId)],
            null,
            [new TileField("rate", "$.rate", "Kurs EUR", [])]);

        var contract = definition.Contract(Connections);

        Assert.That(contract.Parameters, Is.EqualTo(new[]
        {
            new TileContractParameter("date", true),
            new TileContractParameter("currency", false)
        }));
        Assert.That(contract.Inputs, Is.Empty);
        Assert.That(contract.Outputs.Fields, Is.EqualTo(new[] { "Kurs EUR" }));
        Assert.That(contract.Outputs.Items, Is.False);
    }

    [Test(Description = "scenario_TEMP-033 - #9")]
    public void Given_TwoSourcesSharingParameterAndOneBinding_When_ContractIsRead_Then_BoundParameterIsHiddenAndSharedOneIsMerged()
    {
        var definition = new TileDefinition(
            "Zamówienia i kurs",
            TileDisplays.Details,
            [
                new TileSource("rate", TileSourceKinds.Connection, RateConnectionId),
                new TileSource("orders", TileSourceKinds.Connection, OrdersConnectionId)
            ],
            null,
            [new TileField("rate", "$.rate", "Kurs", []), new TileField("orders", "$.total", "Suma", [])])
        {
            Bindings = [new TileBinding("rate", "orders", [new TileBindingMapping("Kurs", "rate", null, null)])]
        };

        var contract = definition.Contract(Connections);

        Assert.That(contract.Parameters, Is.EquivalentTo(new[]
        {
            new TileContractParameter("currency", false),
            new TileContractParameter("date", true)
        }));
    }

    [Test(Description = "scenario_TEMP-034 - #3")]
    public void Given_TileWithUnboundInputSourceAndItems_When_ContractIsRead_Then_InputAndItemOutputsAreDeclared()
    {
        var definition = new TileDefinition(
            "Pozycje",
            TileDisplays.Table,
            [new TileSource("rows", TileSourceKinds.Input, null)],
            new TileItems("rows", "$[*]", "SKU"),
            [new TileField("rows", "$.sku", "SKU", []), new TileField("rows", "$.qty", "Ilość", [])]);

        var contract = definition.Contract(Connections);

        Assert.That(contract.Parameters, Is.Empty);
        Assert.That(contract.Inputs, Is.EqualTo(new[] { "rows" }));
        Assert.That(contract.Outputs, Is.EqualTo(new TileContractOutputs(["SKU", "Ilość"], true)).Using<TileContractOutputs>(
            (left, right) => left.Fields.SequenceEqual(right.Fields) && left.Items == right.Items));
    }

    [Test(Description = "scenario_TEMP-044 - #2")]
    public void Given_TemplateField_When_InputsAreRead_Then_FieldsAreMatchedByNormalizedLabel()
    {
        var address = new TileField(null, null, "Pełny adres", []) { Combination = new TileFieldCombination("template", "{ulica} {Nr}", []) };
        var definition = new TileDefinition(
            "Adres",
            TileDisplays.Details,
            [new TileSource("customer", TileSourceKinds.Connection, RateConnectionId)],
            null,
            [new TileField("customer", "$.street", "Ulica", []), new TileField("customer", "$.number", "Nr", []), address]);

        var inputs = definition.InputsOf(address, Combinations);

        Assert.That(inputs.Select(input => input.Label), Is.EqualTo(new[] { "Ulica", "Nr" }));
    }

    [Test(Description = "scenario_TEMP-033 - #4")]
    public void Given_BindingsFromSourceParentAndChildPath_When_EdgesAreRead_Then_TheyLinkTopLevelNodes()
    {
        var label = new TileField(null, null, "Etykieta", []) { Combination = new TileFieldCombination("join", " ", ["Nazwa", "Kurs"]) };
        var definition = new TileDefinition(
            "Klient",
            TileDisplays.Details,
            [
                new TileSource("customer", TileSourceKinds.Connection, OrdersConnectionId),
                new TileSource("rate", TileSourceKinds.Connection, RateConnectionId)
            ],
            null,
            [new TileField("customer", "$.name", "Nazwa", []), new TileField("rate", "$.rate", "Kurs", []), label])
        {
            Children =
            [
                new TileChild("details", Guid.NewGuid(), 0, null, TileChildScopes.Tile, TileChildLoads.Eager),
                new TileChild("orders", Guid.NewGuid(), 1, null, TileChildScopes.Tile, TileChildLoads.Eager)
            ],
            Bindings =
            [
                new TileBinding("customer", "rate", [new TileBindingMapping("Nazwa", "currency", null, null)]),
                new TileBinding(TileNodeKeys.Parent, "details/summary", [new TileBindingMapping("Etykieta", "label", null, null)]),
                new TileBinding("details/summary", "orders", [new TileBindingMapping("Suma", "total", null, null)])
            ]
        };

        var edges = definition.BindingEdges(Combinations);

        Assert.That(edges, Is.EqualTo(new[]
        {
            ("customer", "rate"),
            ("customer", "details"),
            ("rate", "details"),
            ("details", "orders")
        }));
    }

    [Test(Description = "scenario_TEMP-033 - #9")]
    public void Given_BindingsToNode_When_BoundInputsAreRead_Then_TheyContainMappedParametersAndInputs()
    {
        var definition = new TileDefinition("Klient", TileDisplays.Details, [], null, [])
        {
            Bindings =
            [
                new TileBinding("customer", "orders", [new TileBindingMapping("ID", "customer_id", null, null)]),
                new TileBinding("rate", "orders", [new TileBindingMapping("Kurs", null, "rows", null)]),
                new TileBinding("customer", "rate", [new TileBindingMapping("Waluta", "currency", null, null)])
            ]
        };

        Assert.That(definition.BoundInputs("orders"), Is.EquivalentTo(new[] { "customer_id", "rows" }));
    }

    private static ConnectionDefinition Connections(Guid id)
        => id == RateConnectionId
            ? new GetConnectionDefinition(
                "Kurs walut",
                "https://api.example.com/rates/{date}",
                [new ParameterDeclaration("date", true, null)],
                [new ParameterDeclaration("currency", false, "\"EUR\"")])
            : id == OrdersConnectionId
                ? new GetConnectionDefinition(
                    "Zamówienia",
                    "https://api.example.com/orders/{date}",
                    [new ParameterDeclaration("date", true, null)],
                    [new ParameterDeclaration("rate", true, null)])
                : null;
}
