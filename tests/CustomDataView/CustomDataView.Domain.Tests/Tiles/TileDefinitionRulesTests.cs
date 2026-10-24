using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Tests.Tiles;

public class TileDefinitionRulesTests
{
    private static readonly Guid ConnectionId = Guid.NewGuid();
    private static readonly Guid ChildTileId = Guid.NewGuid();

    [Test(Description = "scenario_TEMP-015-023 - #1")]
    public void Given_DetailsTileWithSourceAndField_When_Checked_Then_ItHasNoViolations()
    {
        Assert.That(Details().Check(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [Test(Description = "scenario_TEMP-015-023 - #7")]
    public void Given_BlankName_When_Checked_Then_NameIsViolated(string name)
    {
        Assert.That((Details() with { Name = name }).Check().Keys, Does.Contain("name"));
    }

    [Test(Description = "scenario_TEMP-015-023 - #8")]
    public void Given_DetailsTileWithoutFields_When_Checked_Then_FieldsAreViolated()
    {
        Assert.That((Details() with { Fields = [] }).Check().Keys, Does.Contain("fields"));
    }

    [Test(Description = "scenario_TEMP-030 - #1")]
    public void Given_LabelRepeatedWithOtherCaseAndSpaces_When_Checked_Then_SecondLabelIsViolated()
    {
        var definition = Details() with { Fields = [Field("customer", "$.name", "Nazwa"), Field("customer", "$.alias", " nazwa ")] };

        Assert.That(definition.Check()["fields[1].label"], Is.EqualTo(new[] { "Etykieta pola musi być unikalna w kafelku." }));
    }

    [TestCase("$.")]
    [TestCase("$.items[?(@.a==)]")]
    [Test(Description = "scenario_TEMP-015-023 - #10")]
    public void Given_InvalidFieldPath_When_Checked_Then_PathIsViolated(string path)
    {
        Assert.That((Details() with { Fields = [Field("customer", path, "Nazwa")] }).Check().Keys, Does.Contain("fields[0].path"));
    }

    [TestCase(null)]
    [TestCase("chart")]
    [Test(Description = "scenario_TEMP-032 - #1")]
    public void Given_UnknownDisplay_When_Checked_Then_DisplayIsViolated(string display)
    {
        Assert.That((Details() with { Display = display }).Check().Keys, Does.Contain("display"));
    }

    [Test(Description = "scenario_TEMP-032 - #4")]
    public void Given_ListWithoutTitleRole_When_Checked_Then_FieldsAreViolated()
    {
        Assert.That((Table() with { Display = TileDisplays.List }).Check().Keys, Does.Contain("fields"));
    }

    [Test(Description = "scenario_TEMP-032 - #4")]
    public void Given_DetailsFieldWithRole_When_Checked_Then_RoleIsViolated()
    {
        Assert.That((Details() with { Fields = [Field("customer", "$.name", "Nazwa") with { Role = TileFieldRoles.Title }] }).Check().Keys, Does.Contain("fields[0].role"));
    }

    [Test(Description = "scenario_TEMP-032 - #6")]
    public void Given_TableWithoutItems_When_Checked_Then_ItemsAreViolated()
    {
        Assert.That((Table() with { Items = null }).Check().Keys, Does.Contain("items"));
    }

    [Test(Description = "scenario_TEMP-032 - #6")]
    public void Given_DetailsWithItems_When_Checked_Then_ItemsAreViolated()
    {
        Assert.That((Details() with { Items = new TileItems("customer", "$[*]", "Nazwa") }).Check().Keys, Does.Contain("items"));
    }

    [Test(Description = "scenario_TEMP-032 - #6")]
    public void Given_TableItemsPathWithoutWildcard_When_Checked_Then_ItemsPathIsViolated()
    {
        Assert.That((Table() with { Items = new TileItems("cities", "$.items", "ID") }).Check().Keys, Does.Contain("items.path"));
    }

    [Test(Description = "scenario_TEMP-032 - #6")]
    public void Given_VisibleFieldOutsideItemsSource_When_Checked_Then_ItsSourceIsViolated()
    {
        var definition = Table() with
        {
            Sources = [Source("cities"), Source("rate")],
            Fields = [Field("cities", "$.id", "ID"), Field("rate", "$.rate", "Kurs")]
        };

        Assert.That((definition).Check().Keys, Does.Contain("fields[1].source"));
        Assert.That((definition with { Fields = [Field("cities", "$.id", "ID"), Field("rate", "$.rate", "Kurs") with { Hidden = true }] }).Check(), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-033 - #2")]
    public void Given_RepeatedSourceKey_When_Checked_Then_SecondKeyIsViolated()
    {
        Assert.That((Details() with { Sources = [Source("customer"), Source("customer")] }).Check().Keys, Does.Contain("sources[1].key"));
    }

    [Test(Description = "scenario_TEMP-033 - #2")]
    public void Given_FieldFromUnknownSource_When_Checked_Then_FieldSourceIsViolated()
    {
        Assert.That((Details() with { Fields = [Field("orders", "$.id", "ID")] }).Check().Keys, Does.Contain("fields[0].source"));
    }

    [Test(Description = "scenario_TEMP-034 - #4")]
    public void Given_InputSourceWithConnection_When_Checked_Then_ConnectionIdIsViolated()
    {
        Assert.That((Details() with { Sources = [new TileSource("customer", TileSourceKinds.Input, ConnectionId)] }).Check().Keys, Does.Contain("sources[0].connectionId"));
    }

    [Test(Description = "scenario_TEMP-035 - #1")]
    public void Given_FormWithTypedFields_When_Checked_Then_ItHasNoViolations()
    {
        Assert.That((Form()).Check(), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-035 - #2")]
    public void Given_FormFieldWithUnknownType_When_Checked_Then_TypeIsViolated()
    {
        Assert.That((Form() with { FormFields = [new TileFormField("Kraj", "color", false, null)] }).Check().Keys, Does.Contain("fields[0].type"));
    }

    [Test(Description = "scenario_TEMP-035 - #2")]
    public void Given_FormFieldDefaultNotMatchingType_When_Checked_Then_DefaultIsViolated()
    {
        Assert.That((Form() with { FormFields = [new TileFormField("Od", TileFormFieldTypes.Date, false, JsonValue.Create("01.01.2026"))] }).Check().Keys, Does.Contain("fields[0].default"));
    }

    [Test(Description = "scenario_TEMP-035 - #2")]
    public void Given_FormWithSourcesItemsAndBindings_When_Checked_Then_EachIsViolated()
    {
        var definition = Form() with
        {
            Sources = [Source("customer")],
            Items = new TileItems("customer", "$[*]", "Kraj"),
            Bindings = [new TileBinding("customer", "orders", [new TileBindingMapping("Kraj", "country", null, null)])]
        };

        Assert.That(definition.Check().Keys, Is.SupersetOf(new[] { "sources", "items", "bindings" }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_FormWithAction_When_Checked_Then_ActionsAreViolated()
    {
        Assert.That((Form() with { Actions = [Action("open", TileActionScopes.Tile)] }).Check().Keys, Does.Contain("actions"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_FormWithPagination_When_Checked_Then_PaginationIsViolated()
    {
        Assert.That((Form() with { Pagination = new TilePagination("pageNumber", "pageSize", 10, null) }).Check().Keys, Does.Contain("pagination"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #1")]
    public void Given_TabsWithTitledChildren_When_Checked_Then_ItHasNoViolations()
    {
        Assert.That((Tabs()).Check(), Is.Empty);
    }

    [Test(Description = "scenario_TEMP-041-043 - #5")]
    public void Given_TabsWithoutChildren_When_Checked_Then_ChildrenAreViolated()
    {
        Assert.That((Tabs() with { Children = [] }).Check().Keys, Does.Contain("children"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #5")]
    public void Given_TabWithoutTitle_When_Checked_Then_ItsLabelIsViolated()
    {
        Assert.That((Tabs() with { Children = [Child("filters", 0)] }).Check().Keys, Does.Contain("children[0].label"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #5")]
    public void Given_TabsWithItemScopedTab_When_Checked_Then_ScopeIsViolated()
    {
        Assert.That((Tabs() with { Children = [Child("filters", 0, TileChildScopes.Item) with { Label = "Filtry" }] }).Check().Keys, Does.Contain("children[0].scope"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #5")]
    public void Given_TabsWithOwnFieldsAndSources_When_Checked_Then_FieldsAndSourcesAreViolated()
    {
        var definition = Tabs() with { Sources = [Source("customer")], Fields = [Field("customer", "$.name", "Nazwa")] };

        Assert.That(definition.Check().Keys, Is.SupersetOf(new[] { "fields", "sources" }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #14")]
    public void Given_TabsWithPagination_When_Checked_Then_PaginationIsViolated()
    {
        Assert.That((Tabs() with { Pagination = new TilePagination("page", "limit", 10, null) }).Check().Keys, Does.Contain("pagination"));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_ChildKeyEqualToSourceKey_When_Checked_Then_ChildKeyIsViolated()
    {
        Assert.That((Details() with { Children = [Child("customer", 0)] }).Check().Keys, Does.Contain("children[0].key"));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_RepeatedChildPosition_When_Checked_Then_SecondPositionIsViolated()
    {
        Assert.That((Details() with { Children = [Child("orders", 1), Child("invoices", 1)] }).Check().Keys, Does.Contain("children[1].position"));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_ChildWithoutTile_When_Checked_Then_TileIdIsViolated()
    {
        Assert.That((Details() with { Children = [Child("orders", 0) with { TileId = Guid.Empty }] }).Check().Keys, Does.Contain("children[0].tileId"));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_ItemScopedChildWithoutItems_When_Checked_Then_ScopeIsViolated()
    {
        Assert.That((Details() with { Children = [Child("orders", 0, TileChildScopes.Item)] }).Check().Keys, Does.Contain("children[0].scope"));
    }

    [Test(Description = "scenario_TEMP-036 - #5")]
    public void Given_ChildWithUnknownLoadAndLongCssClass_When_Checked_Then_BothAreViolated()
    {
        var definition = Details() with
        {
            Children = [Child("orders", 0) with { Load = "later", CssClass = new string('x', TileTexts.MaxCssClassLength + 1) }]
        };

        Assert.That(definition.Check().Keys, Is.SupersetOf(new[] { "children[0].load", "children[0].cssClass" }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_ItemActionInTileWithoutItems_When_Checked_Then_ScopeIsViolated()
    {
        Assert.That((Details() with { Actions = [Action("open", TileActionScopes.Item)] }).Check().Keys, Does.Contain("actions[0].scope"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_RepeatedActionKey_When_Checked_Then_SecondKeyIsViolated()
    {
        Assert.That((Details() with { Actions = [Action("open", TileActionScopes.Tile), Action("open", TileActionScopes.Tile)] }).Check().Keys, Does.Contain("actions[1].key"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_ItemActionMappingTileField_When_Checked_Then_FieldIsViolated()
    {
        var definition = Table() with
        {
            Sources = [Source("cities"), Source("rate")],
            Fields = [Field("cities", "$.id", "ID"), Field("rate", "$.rate", "Kurs") with { Hidden = true }],
            Actions = [Action("open", TileActionScopes.Item) with { Map = [new TileActionMapping("Kurs", "rate", null)] }]
        };

        Assert.That(definition.Check()["actions[0].map[0].field"], Is.EqualTo(new[] { "Pole 'Kurs' nie jest polem elementu." }));
    }

    [Test(Description = "scenario_TEMP-041-043 - #20")]
    public void Given_ActionMappingWithParameterAndInput_When_Checked_Then_MappingIsViolated()
    {
        var definition = Details() with { Actions = [Action("open", TileActionScopes.Tile) with { Map = [new TileActionMapping("Nazwa", "name", "rows")] }] };

        Assert.That((definition).Check().Keys, Does.Contain("actions[0].map[0]"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #9")]
    public void Given_PaginationTableWithConnectionItems_When_Checked_Then_ItHasNoViolations()
    {
        Assert.That((PaginationTable()).Check(), Is.Empty);
    }

    [TestCase(0)]
    [TestCase(TilePagination.MaxPageSize + 1)]
    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_PageSizeOutOfRange_When_Checked_Then_PageSizeIsViolated(int pageSize)
    {
        Assert.That((PaginationTable() with { Pagination = new TilePagination("page", "limit", pageSize, null) }).Check().Keys, Does.Contain("pagination.pageSize"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_SamePageParameterNames_When_Checked_Then_PageSizeParameterIsViolated()
    {
        Assert.That((PaginationTable() with { Pagination = new TilePagination("page", "page", 10, null) }).Check().Keys, Does.Contain("pagination.pageSizeParameter"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_TotalPathWithWildcard_When_Checked_Then_TotalPathIsViolated()
    {
        Assert.That((PaginationTable() with { Pagination = new TilePagination("page", "limit", 10, "$.items[*]") }).Check().Keys, Does.Contain("pagination.totalPath"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #13")]
    public void Given_PaginationTableWithInputItems_When_Checked_Then_PaginationIsViolated()
    {
        Assert.That((PaginationTable() with { Sources = [new TileSource("cities", TileSourceKinds.Input, null)] }).Check().Keys, Does.Contain("pagination"));
    }

    [Test(Description = "scenario_TEMP-041-043 - #14")]
    public void Given_TableWithPagination_When_Checked_Then_PaginationIsViolated()
    {
        Assert.That((Table() with { Pagination = new TilePagination("page", "limit", 10, null) }).Check().Keys, Does.Contain("pagination"));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_BindingToSameNode_When_Checked_Then_TargetIsViolated()
    {
        Assert.That((Details() with { Bindings = [Binding("customer", "customer")] }).Check().Keys, Does.Contain("bindings[0].to"));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_RepeatedBinding_When_Checked_Then_SecondBindingIsViolated()
    {
        Assert.That((Details() with { Bindings = [Binding("customer", "orders"), Binding("customer", "orders")] }).Check().Keys, Does.Contain("bindings[1]"));
    }

    [Test(Description = "scenario_TEMP-033 - #5")]
    public void Given_BindingWithoutMappings_When_Checked_Then_MapIsViolated()
    {
        Assert.That((Details() with { Bindings = [new TileBinding("customer", "orders", [])] }).Check().Keys, Does.Contain("bindings[0].map"));
    }

    [Test(Description = "scenario_TEMP-039 - #4")]
    public void Given_BindingMappingWithUnknownScope_When_Checked_Then_ScopeIsViolated()
    {
        var definition = Details() with { Bindings = [new TileBinding("customer", "orders", [new TileBindingMapping("Nazwa", "name", null, "some")])] };

        Assert.That((definition).Check().Keys, Does.Contain("bindings[0].map[0].scope"));
    }

    [Test(Description = "scenario_TEMP-044 - #11")]
    public void Given_CombinedFieldWithSourceAndPath_When_Checked_Then_FieldIsViolated()
    {
        var combined = Field("customer", "$.name", "Pełny adres") with { Combination = new TileFieldCombination("join", " ", ["Nazwa"]) };

        Assert.That((Details() with { Fields = [Field("customer", "$.name", "Nazwa"), combined] }).Check().Keys, Does.Contain("fields[1]"));
    }

    private static TileDefinition Details()
        => new("Klient", TileDisplays.Details, [Source("customer")], null, [Field("customer", "$.name", "Nazwa")]);

    private static TileDefinition Table()
        => new(
            "Miasta",
            TileDisplays.Table,
            [Source("cities")],
            new TileItems("cities", "$.items[*]", "ID"),
            [Field("cities", "$.id", "ID"), Field("cities", "$.name", "Nazwa")]);

    private static TileDefinition PaginationTable()
        => Table() with { Display = TileDisplays.PaginationTable, Pagination = new TilePagination("page", "limit", 10, "$.total") };

    private static TileDefinition Form()
        => new TileDefinition("Filtry", TileDisplays.Form, [], null, [])
        {
            FormFields =
            [
                new TileFormField("Kraj", TileFormFieldTypes.Text, true, null),
                new TileFormField("Od", TileFormFieldTypes.Date, false, JsonValue.Create("2026-01-01"))
            ]
        };

    private static TileDefinition Tabs()
        => new TileDefinition("Wyszukiwarka", TileDisplays.Tabs, [], null, [])
        {
            Children = [Child("filters", 0) with { Label = "Filtry" }, Child("results", 1) with { Label = "Wyniki" }]
        };

    private static TileSource Source(string key) => new(key, TileSourceKinds.Connection, ConnectionId);

    private static TileField Field(string source, string path, string label) => new(source, path, label, []);

    private static TileChild Child(string key, int position, string scope = TileChildScopes.Tile)
        => new(key, ChildTileId, position, null, scope, TileChildLoads.Eager);

    private static TileAction Action(string key, string scope)
        => new(key, "Otwórz", TileActionTypes.Button, ChildTileId, scope, []);

    private static TileBinding Binding(string from, string to)
        => new(from, to, [new TileBindingMapping("Nazwa", "name", null, null)]);
}
