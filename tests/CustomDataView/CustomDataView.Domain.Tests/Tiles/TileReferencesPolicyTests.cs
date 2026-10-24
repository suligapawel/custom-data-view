using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Combinations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class TileReferencesPolicyTests
{
    private static readonly Guid CustomerConnectionId = Guid.NewGuid();
    private static readonly Guid OrdersConnectionId = Guid.NewGuid();

    private readonly Dictionary<Guid, TileDefinition> tiles = [];
    private readonly Dictionary<Guid, ConnectionDefinition> connections = new()
    {
        [CustomerConnectionId] = new GetConnectionDefinition(
            "Klient",
            "https://api.example.com/customers/{customer_id}",
            [new ParameterDeclaration("customer_id", true, null)],
            [new ParameterDeclaration("page", false, null), new ParameterDeclaration("limit", false, null)]),
        [OrdersConnectionId] = new GetConnectionDefinition(
            "Zamówienia",
            "https://api.example.com/orders",
            [],
            [new ParameterDeclaration("customer", true, null), new ParameterDeclaration("region", false, null)])
    };

    [Test(Description = "scenario_TEMP-033 - #1")]
    public void Given_TileWithExistingConnectionsAndBinding_When_Checked_Then_ItHasNoViolations()
    {
        Assert.That(Check(CustomerWithOrders()), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-015-023 - #11")]
    public void Given_SourceWithMissingConnection_When_Checked_Then_ConnectionIdIsViolated()
    {
        var missing = Guid.NewGuid();

        var violations = Check(Customer() with { Sources = [new TileSource("customer", TileSourceKinds.Connection, missing)] });

        Assert.That(violations["sources[0].connectionId"], Is.EqualTo(new[] { $"Połączenie '{missing}' nie istnieje." }));
    }

    [Test(Description = "scenario_TEMP-015-023 - #12")]
    public void Given_FieldWithUnknownTransformation_When_Checked_Then_TransformationKeyIsViolated()
    {
        var definition = Customer() with { Fields = [new TileField("customer", "$.name", "Nazwa", [new TransformationStep("reverse")])] };

        Assert.That(Check(definition).Keys, Does.Contain("fields[0].transformations[0].key"));
    }

    [Test(Description = "scenario_TEMP-044 - #11")]
    public void Given_CombinedFieldUsingItself_When_Checked_Then_CombinationIsViolated()
    {
        var definition = Customer() with
        {
            Fields =
            [
                new TileField("customer", "$.name", "Nazwa", []),
                new TileField(null, null, "Pełna nazwa", []) { Combination = new TileFieldCombination("join", " ", ["Nazwa", "Pełna nazwa"]) }
            ]
        };

        Assert.That(Check(definition)["fields[1].combine"], Is.EqualTo(new[] { "Pole 'Pełna nazwa' nie może składać się z samego siebie." }));
    }

    [Test(Description = "scenario_TEMP-044 - #11")]
    public void Given_TemplateOverListField_When_Checked_Then_CombinationIsViolated()
    {
        var definition = Customer() with
        {
            Fields =
            [
                new TileField("customer", "$.tags[*]", "Tagi", []),
                new TileField(null, null, "Opis", []) { Combination = new TileFieldCombination("template", "{Tagi}", []) }
            ]
        };

        Assert.That(Check(definition).Keys, Does.Contain("fields[1].combine"));
    }

    [Test(Description = "scenario_TEMP-044 - #11")]
    public void Given_UnknownCombination_When_Checked_Then_CombinationKeyIsViolated()
    {
        var definition = Customer() with
        {
            Fields = [new TileField(null, null, "Opis", []) { Combination = new TileFieldCombination("reverse", null, []) }]
        };

        Assert.That(Check(definition).Keys, Does.Contain("fields[0].combine.key"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_PageParameterNotDeclaredByItemsConnection_When_Checked_Then_PageNumberParameterIsViolated()
    {
        var definition = new TileDefinition(
            "Klienci",
            TileDisplays.PaginationTable,
            [new TileSource("customers", TileSourceKinds.Connection, CustomerConnectionId)],
            new TileItems("customers", "$.items[*]", "ID"),
            [new TileField("customers", "$.id", "ID", [])])
        {
            Pagination = new TilePagination("pageNumber", "limit", 10, null)
        };

        Assert.That(Check(definition).Keys, Is.EqualTo(new[] { "pagination.pageNumberParameter" }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_ActionToMissingTileOrUndeclaredParameter_When_Checked_Then_TargetAndParameterAreViolated()
    {
        var orders = Add(Orders());
        var definition = Customer() with
        {
            Actions =
            [
                new TileAction("open", "Otwórz", TileActionTypes.Button, Guid.NewGuid(), TileActionScopes.Tile, []),
                new TileAction("orders", "Zamówienia", TileActionTypes.Button, orders, TileActionScopes.Tile, [new TileActionMapping("Nazwa", "status", null)])
            ]
        };

        Assert.That(Check(definition).Keys, Is.EquivalentTo(new[] { "actions[0].tileId", "actions[1].map[0].parameter" }));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_ChildPointingAtMissingTile_When_Checked_Then_ChildTileIsViolated()
    {
        var definition = Customer() with { Children = [Child("orders", Guid.NewGuid())] };

        Assert.That(Check(definition).Keys, Is.EqualTo(new[] { "children[0].tileId" }));
    }

    [Test(Description = "scenario_TEMP-036 - #6")]
    public void Given_CarrierEmbeddedInShipping_When_CarrierEmbedsShipping_Then_CycleIsViolated()
    {
        var carrier = Add(Customer() with { Name = "Przewoźnik" });
        var shipping = Add(Customer() with { Name = "Dostawa", Children = [Child("carrier", carrier)] });

        var violations = Check(tiles[carrier] with { Children = [Child("shipping", shipping)] }, carrier);

        Assert.That(violations["children[0].tileId"], Is.EqualTo(new[] { "Osadzenie tworzy cykl zawierania kafelków." }));
    }

    [Test(Description = "scenario_TEMP-036 - #2")]
    public void Given_FormEmbeddedWithItemScope_When_Checked_Then_ChildScopeIsViolated()
    {
        var form = Add(new TileDefinition("Filtr", TileDisplays.Form, [], null, []) { FormFields = [new TileFormField("Status", TileFormFieldTypes.Text, false, null)] });

        var violations = Check(Customer() with { Children = [Child("filter", form, TileChildScopes.Item)] });

        Assert.That(violations.Keys, Is.EqualTo(new[] { "children[0].scope" }));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_SourceBindingToUndeclaredParameter_When_Checked_Then_ParameterIsViolated()
    {
        var definition = CustomerWithOrders() with
        {
            Bindings = [new TileBinding("customer", "orders", [new TileBindingMapping("ID", "country", null, null)])]
        };

        Assert.That(Check(definition).Keys, Is.EqualTo(new[] { "bindings[0].map[0].parameter" }));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_BindingsFormingCycle_When_Checked_Then_BindingsAreViolated()
    {
        var definition = CustomerWithOrders() with
        {
            Fields = [new TileField("customer", "$.id", "ID", []), new TileField("orders", "$.customer", "Klient", [])],
            Bindings =
            [
                new TileBinding("customer", "orders", [new TileBindingMapping("ID", "customer", null, null)]),
                new TileBinding("orders", "customer", [new TileBindingMapping("Klient", "customer_id", null, null)])
            ]
        };

        Assert.That(Check(definition)["bindings"], Is.EqualTo(new[] { "Wiązania tworzą cykl między węzłami: customer, orders." }));
    }

    [Test(Description = "scenario_TEMP-036 - #7")]
    public void Given_ParentBindingToParameterOutsideChildContract_When_Checked_Then_ParameterIsViolated()
    {
        var orders = Add(Orders());
        var definition = Customer() with
        {
            Children = [Child("orders", orders)],
            Bindings = [new TileBinding(TileNodeKeys.Parent, "orders", [new TileBindingMapping("Nazwa", "status", null, null)])]
        };

        Assert.That(Check(definition)["bindings[0].map[0].parameter"], Is.EqualTo(new[] { "Parametr 'status' nie jest wejściem kafelka 'orders'." }));
    }

    [Test(Description = "scenario_TEMP-036 - #7")]
    public void Given_GrandchildParameterBoundByChild_When_ParentBindsIt_Then_ParameterIsViolated()
    {
        var invoices = Add(Orders() with { Name = "Faktury" });
        var orders = Add(Customer() with
        {
            Name = "Zamówienia klienta",
            Children = [Child("invoices", invoices)],
            Bindings = [new TileBinding(TileNodeKeys.Parent, "invoices", [new TileBindingMapping("Nazwa", "customer", null, null)])]
        });
        var definition = Customer() with
        {
            Children = [Child("orders", orders)],
            Bindings = [new TileBinding(TileNodeKeys.Parent, "orders/invoices", [new TileBindingMapping("Nazwa", "customer", null, null)])]
        };

        Assert.That(Check(definition)["bindings[0].map[0].parameter"], Is.EqualTo(new[]
        {
            "Wejście 'customer' kafelka 'orders/invoices' jest już związane przez kafelek 'orders'."
        }));
    }

    [Test(Description = "scenario_TEMP-036 - #7")]
    public void Given_ParentBindingToSource_When_Checked_Then_PairIsViolated()
    {
        var definition = CustomerWithOrders() with
        {
            Bindings = [new TileBinding(TileNodeKeys.Parent, "orders", [new TileBindingMapping("ID", "customer", null, null)])]
        };

        Assert.That(Check(definition).Keys, Is.EqualTo(new[] { "bindings[0].to" }));
    }

    [Test(Description = "scenario_TEMP-037 - #3")]
    public void Given_ItemScopeBindingToTileScopedChild_When_Checked_Then_ScopeIsViolated()
    {
        var orders = Add(Orders());
        var definition = new TileDefinition(
            "Klienci",
            TileDisplays.Table,
            [new TileSource("customers", TileSourceKinds.Connection, CustomerConnectionId)],
            new TileItems("customers", "$.items[*]", "ID"),
            [new TileField("customers", "$.id", "ID", [])])
        {
            Children = [Child("orders", orders)],
            Bindings = [new TileBinding(TileNodeKeys.Parent, "orders", [new TileBindingMapping("ID", "customer", null, BindingScopes.Item)])]
        };

        Assert.That(Check(definition).Keys, Is.EqualTo(new[] { "bindings[0].map[0].scope" }));
    }

    private IReadOnlyDictionary<string, string[]> Check(TileDefinition definition, Guid? tileId = null)
        => new TileReferencesPolicy(
                new TileTree(id => tiles.GetValueOrDefault(id)),
                id => connections.GetValueOrDefault(id),
                new HashSet<string>(["uppercase"], StringComparer.Ordinal),
                new CombinationService([new JoinCombination(), new TemplateCombination()]))
            .Check(definition, tileId);

    private Guid Add(TileDefinition definition)
    {
        var id = Guid.NewGuid();
        tiles[id] = definition;
        return id;
    }

    private static TileDefinition Customer()
        => new(
            "Klient",
            TileDisplays.Details,
            [new TileSource("customer", TileSourceKinds.Connection, CustomerConnectionId)],
            null,
            [new TileField("customer", "$.name", "Nazwa", [new TransformationStep("uppercase")])]);

    private static TileDefinition Orders()
        => new(
            "Zamówienia",
            TileDisplays.Details,
            [new TileSource("orders", TileSourceKinds.Connection, OrdersConnectionId)],
            null,
            [new TileField("orders", "$.total", "Suma", [])]);

    private static TileDefinition CustomerWithOrders()
        => new TileDefinition(
            "Klient z zamówieniami",
            TileDisplays.Details,
            [
                new TileSource("customer", TileSourceKinds.Connection, CustomerConnectionId),
                new TileSource("orders", TileSourceKinds.Connection, OrdersConnectionId)
            ],
            null,
            [new TileField("customer", "$.id", "ID", []), new TileField("orders", "$.total", "Suma", [])])
        {
            Bindings = [new TileBinding("customer", "orders", [new TileBindingMapping("ID", "customer", null, null)])]
        };

    private static TileChild Child(string key, Guid tileId, string scope = TileChildScopes.Tile)
        => new(key, tileId, 0, null, scope, TileChildLoads.Eager);
}
