using CustomDataView.Domain.Dictionaries;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Repositories;
using Npgsql;

namespace CustomDataView.Infrastructure.Tests;

public class PostgresTileRepositoryTests
{
    private NpgsqlDataSource dataSource;
    private PostgresTileRepository repository;
    private Guid customerConnectionId;
    private Guid rateConnectionId;

    [OneTimeSetUp]
    public void CreateDatabase()
    {
        dataSource = TestDatabase.Create();
        repository = new PostgresTileRepository(TestDatabase.ContextFactory(dataSource));
    }

    [OneTimeTearDown]
    public void DisposeDatabase()
    {
        dataSource?.Dispose();
    }

    [SetUp]
    public void ClearDatabase()
    {
        TestDatabase.Clear(dataSource);
        customerConnectionId = AddConnection("Klient");
        rateConnectionId = AddConnection("Kurs walut");
    }

    [Test]
    public void Given_TwoSavedTiles_When_AllAreLoaded_Then_TheyAreInAddingOrder()
    {
        var first = Tile.Restore(Guid.NewGuid(), Definition("Klient", []));
        var second = Tile.Restore(Guid.NewGuid(), Definition("Kurs", []));

        repository.Add(first);
        repository.Add(second);

        Assert.That(repository.GetAll().Select(tile => (tile.Id, tile.Definition.Name)), Is.EqualTo(new[]
        {
            (first.Id, "Klient"),
            (second.Id, "Kurs")
        }));
    }

    [Test]
    public void Given_SavedTileWithFieldsFromManyConnections_When_ItIsLoaded_Then_FieldsAreInDefinitionOrder()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Klient i kurs",
        [
            new TileField("rate", "$.rate", "Kurs EUR", []),
            new TileField("customer", "$.name", "Nazwa", []),
            new TileField("customer", "$['unit-name']", "Dział", [], Hidden: true)
        ]));

        repository.Add(tile);

        var loaded = repository.Get(tile.Id);
        Assert.That(Json(loaded), Is.EqualTo(Json(tile)));
    }

    [Test]
    public void Given_FieldWithTransformationChain_When_TileIsLoaded_Then_KeysOrderAndParametersAreUnchanged()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Klient",
        [
            new TileField("customer", "$.balance", "Saldo",
            [
                new TransformationStep("round", "2"),
                new TransformationStep("suffix", " zł"),
                new TransformationStep("uppercase"),
                new TransformationStep("round", "dwa")
            ])
        ]));

        repository.Add(tile);

        Assert.That(Json(repository.Get(tile.Id)), Is.EqualTo(Json(tile)));
    }

    [Test]
    public void Given_TwoSavedTiles_When_FirstIsUpdated_Then_ItsDefinitionIsReplacedAndOrderIsKept()
    {
        var first = Tile.Restore(Guid.NewGuid(), Definition("Klient",
        [
            new TileField("customer", "$.name", "Nazwa",
                [new TransformationStep("round", "2")]),
            new TileField("customer", "$.balance", "Saldo", [])
        ]));
        var second = Tile.Restore(Guid.NewGuid(), Definition("Kurs", [new TileField("rate", "$.rate", "Kurs EUR", [])]));
        repository.Add(first);
        repository.Add(second);
        var updated = Tile.Restore(first.Id, Definition("Klient VIP",
            [
                new TileField("rate", "$.address.city", "Miasto",
                    [new TransformationStep("suffix", " zł")])
            ]));

        repository.Update(updated);

        Assert.That(repository.GetAll().Select(Json), Is.EqualTo(new[] { Json(updated), Json(second) }));
    }

    [Test]
    public void Given_SavedTileWithTransformations_When_ItIsDeleted_Then_ItIsGoneAndConnectionsRemain()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Klient",
        [
            new TileField("customer", "$.balance", "Saldo",
                [new TransformationStep("round", "2")])
        ]));
        repository.Add(tile);

        repository.Delete(tile.Id);

        Assert.That(repository.Get(tile.Id), Is.Null);
        Assert.That(repository.GetAll(), Is.Empty);
        Assert.That(new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Get(customerConnectionId), Is.Not.Null);
    }

    [Test]
    public void Given_TilesUsingConnections_When_TilesUsingConnectionAreLoaded_Then_DistinctTilesAreReturnedInAddingOrder()
    {
        var customer = Tile.Restore(Guid.NewGuid(), Definition("Klient",
        [
            new TileField("customer", "$.name", "Nazwa", []),
            new TileField("customer", "$.balance", "Saldo", [])
        ]));
        var rate = Tile.Restore(Guid.NewGuid(), Definition("Kurs", [new TileField("rate", "$.rate", "Kurs EUR", [])]));
        var both = Tile.Restore(Guid.NewGuid(), Definition("Klient i kurs",
        [
            new TileField("rate", "$.rate", "Kurs EUR", []),
            new TileField("customer", "$.name", "Nazwa", [])
        ]));
        repository.Add(customer);
        repository.Add(rate);
        repository.Add(both);

        Assert.That(
            repository.GetUsingConnection(customerConnectionId),
            Is.EqualTo(new[] { new TileReference(customer.Id, "Klient"), new TileReference(both.Id, "Klient i kurs") }));
    }

    [Test]
    public void Given_TileUsingConnection_When_ConnectionIsDeletedDirectly_Then_DatabaseRejectsIt()
    {
        repository.Add(Tile.Restore(Guid.NewGuid(), Definition("Klient", [new TileField("customer", "$.name", "Nazwa", [])])));

        Assert.Throws<PostgresException>(() => new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Delete(customerConnectionId));
    }

    [Test]
    public void Given_ListTileWithItemsAndRoles_When_ItIsSavedAndUpdatedToSummary_Then_DisplayItemsAndRolesAreKept()
    {
        var list = Tile.Restore(Guid.NewGuid(), Definition("Miasta", [
            new TileField("customer", "$.id", "ID", []),
            new TileField("customer", "$.name", "Miasto", [], Role: TileFieldRoles.Title)
        ]) with { Display = TileDisplays.List, Items = new TileItems("customer", "$.cities[*]", "ID") });
        repository.Add(list);
        var loaded = repository.Get(list.Id);
        var summary = Tile.Restore(list.Id, Definition("Saldo", [new TileField("rate", "$.rate", "Kurs", [], Role: TileFieldRoles.Primary)]) with
        {
            Display = TileDisplays.Summary
        });

        repository.Update(summary);

        Assert.That(Json(loaded), Is.EqualTo(Json(list)));
        Assert.That(Json(repository.Get(list.Id)), Is.EqualTo(Json(summary)));
    }

    [Test]
    public void Given_TileWithBindingsHavingManyMappings_When_ItIsLoaded_Then_BindingsAreGroupedInDefinitionOrder()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Klient i kurs", [
            new TileField("customer", "$.id", "ID", [], Hidden: true),
            new TileField("customer", "$.currency", "Waluta", [], Hidden: true),
            new TileField("rate", "$.rate", "Kurs", [])
        ]) with
        {
            Bindings =
            [
                new TileBinding("customer", "rate", [
                    new TileBindingMapping("Waluta", "currency", null, null),
                    new TileBindingMapping("ID", "customer", null, null)
                ])
            ]
        });

        repository.Add(tile);

        Assert.That(Json(repository.Get(tile.Id)), Is.EqualTo(Json(tile)));
    }

    [Test]
    public void Given_FormTile_When_ItIsLoaded_Then_FormFieldsWithTypesAndDefaultsAreKept()
    {
        var form = Tile.Restore(Guid.NewGuid(), new TileDefinition("Filtry", TileDisplays.Form, [], null, [])
        {
            FormFields =
            [
                new TileFormField("Kraj", TileFormFieldTypes.Text, true, null),
                new TileFormField("Limit", TileFormFieldTypes.Number, false, JsonValue.Create(20)),
                new TileFormField("Aktywne", TileFormFieldTypes.Boolean, false, JsonValue.Create(true))
            ]
        });

        repository.Add(form);

        Assert.That(Json(repository.Get(form.Id)), Is.EqualTo(Json(form)));
    }

    [Test]
    public void Given_TileWithChildrenAndChildBinding_When_ParentsAndFieldUsesAreLoaded_Then_ParentAndBoundFieldAreReturned()
    {
        var child = Tile.Restore(Guid.NewGuid(), Definition("Klient", [new TileField("customer", "$.id", "ID", [])]));
        var parent = Tile.Restore(Guid.NewGuid(), Definition("Kurs klienta", [new TileField("rate", "$.rate", "Kurs", [])]) with
    {
        Children =
        [
            new TileChild("customer", child.Id, 1, "col-6", TileChildScopes.Tile, TileChildLoads.Eager),
            new TileChild("other", child.Id, 0, null, TileChildScopes.Tile, TileChildLoads.Lazy)
        ],
        Bindings = [new TileBinding("customer", "rate", [new TileBindingMapping("ID", "currency", null, null)])]
    });
        repository.Add(child);

        repository.Add(parent);

        Assert.That(Json(repository.Get(parent.Id)), Is.EqualTo(Json(Tile.Restore(parent.Id, parent.Definition with { Children = [.. parent.Definition.Children.OrderBy(embedded => embedded.Position)] }))));
        Assert.That(repository.GetParents(child.Id), Is.EqualTo(new[] { new TileReference(parent.Id, "Kurs klienta") }));
        Assert.That(repository.GetFieldUses(child.Id), Is.EqualTo(new[] { new TileFieldUse(new TileReference(parent.Id, "Kurs klienta"), "ID") }));
        Assert.That(() => repository.Delete(child.Id), Throws.InstanceOf<PostgresException>());
    }

    [Test]
    public void Given_PaginationTableWithActionAndTabsWithTitledTab_When_TheyAreSavedAndUpdated_Then_DefinitionsRoundTrip()
    {
        var target = Tile.Restore(Guid.NewGuid(), Definition("Klient", [new TileField("customer", "$.id", "ID", [])]));
        var results = Tile.Restore(Guid.NewGuid(), new TileDefinition(
            "Wyniki",
            TileDisplays.PaginationTable,
            [new TileSource("customer", TileSourceKinds.Connection, customerConnectionId)],
            new TileItems("customer", "$.items[*]", "ID"),
            [new TileField("customer", "$.id", "ID", [])])
        {
            Pagination = new TilePagination("pageNumber", "pageSize", 10, "$.total"),
            Actions =
            [
                new TileAction("details", "Szczegóły", TileActionTypes.Button, target.Id, TileActionScopes.Item, [new TileActionMapping("ID", "customer_id", null)]),
                new TileAction("self", "Odśwież", TileActionTypes.Link, Guid.Empty, TileActionScopes.Tile, [])
            ]
        });
        results = Tile.Restore(results.Id, results.Definition with { Actions = [results.Definition.Actions[0], results.Definition.Actions[1] with { TileId = results.Id }] });
        var tabs = Tile.Restore(Guid.NewGuid(), new TileDefinition("Filtry", TileDisplays.Tabs, [], null, [])
        {
            Children = [new TileChild("results", results.Id, 0, null, TileChildScopes.Tile, TileChildLoads.Lazy, "Wyniki")]
        });
        repository.Add(target);
        repository.Add(results);
        repository.Add(tabs);

        Assert.That(Json(repository.Get(results.Id)), Is.EqualTo(Json(results)));
        Assert.That(Json(repository.Get(tabs.Id)), Is.EqualTo(Json(tabs)));
        Assert.That(repository.GetActionReferences(target.Id), Is.EqualTo(new[] { new TileReference(results.Id, "Wyniki") }));
        Assert.That(repository.GetActionReferences(results.Id), Is.Empty);
        Assert.That(() => repository.Delete(target.Id), Throws.InstanceOf<PostgresException>());

        var updated = Tile.Restore(results.Id, results.Definition with { Pagination = new TilePagination("page", "limit", 5, null), Actions = [] });
        repository.Update(updated);

        Assert.That(Json(repository.Get(results.Id)), Is.EqualTo(Json(updated)));
        Assert.That(repository.GetActionReferences(target.Id), Is.Empty);
    }

    [Test]
    public void Given_GrandparentBindingFromDescendantPath_When_FieldUsesAreLoaded_Then_GrandparentUseIsReturned()
    {
        var form = Tile.Restore(Guid.NewGuid(), Definition("Klient", [new TileField("customer", "$.id", "ID", [])]));
        var tabs = Tile.Restore(Guid.NewGuid(), new TileDefinition("Filtry", TileDisplays.Tabs, [], null, [])
        {
            Children = [new TileChild("byId", form.Id, 0, null, TileChildScopes.Tile, TileChildLoads.Eager, "Po ID")]
        });
        var search = Tile.Restore(Guid.NewGuid(), Definition("Szukaj", [new TileField("rate", "$.rate", "Kurs", [])]) with
    {
        Children = [new TileChild("filters", tabs.Id, 0, null, TileChildScopes.Tile, TileChildLoads.Eager)],
        Bindings = [new TileBinding("filters/byId", "rate", [new TileBindingMapping("ID", "currency", null, null)])]
    });
        repository.Add(form);
        repository.Add(tabs);
        repository.Add(search);

        Assert.That(repository.GetFieldUses(form.Id), Is.EqualTo(new[] { new TileFieldUse(new TileReference(search.Id, "Szukaj"), "ID") }));
        Assert.That(repository.GetFieldUses(tabs.Id), Is.Empty);
    }

    [Test]
    public void Given_TileWithLabelsDifferingInCaseAndSpaces_When_ItIsSaved_Then_DatabaseRejectsIt()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Klient",
        [
            new TileField("customer", "$.name", "Nazwa", []),
            new TileField("customer", "$.balance", " NAZWA ", [])
        ]));

        Assert.That(() => repository.Add(tile), Throws.InstanceOf<Microsoft.EntityFrameworkCore.DbUpdateException>());
        Assert.That(repository.Get(tile.Id), Is.Null);
    }

    [Test]
    public void Given_TilesMappingWithDictionaries_When_DictionaryUsesAreLoaded_Then_DistinctTilesAndValueLabelsOfThatDictionaryAreReturned()
    {
        var dictionary = Tile.Restore(Guid.NewGuid(), Definition("Statusy", []));
        var otherDictionary = Tile.Restore(Guid.NewGuid(), Definition("Poziomy", []));
        var orders = Tile.Restore(Guid.NewGuid(), Definition("Zamówienia",
        [
            new TileField("customer", "$.status", "Status", [new TransformationStep("mapper", $"{dictionary.Id}:Tłumaczenie")]),
            new TileField("customer", "$.previous", "Poprzedni", [new TransformationStep("mapper", $"{dictionary.Id}: tłumaczenie ")]),
            new TileField("customer", "$.weight", "Waga", [new TransformationStep("mapper", $"{dictionary.Id}:Waga")]),
            new TileField("customer", "$.level", "Poziom", [new TransformationStep("mapper", $"{otherDictionary.Id}:Na")])
        ]));
        var invoices = Tile.Restore(Guid.NewGuid(), Definition("Faktury",
        [
            new TileField("customer", "$.status", "Status", [new TransformationStep("uppercase"), new TransformationStep("mapper", $"{dictionary.Id}:Tłumaczenie")])
        ]));
        repository.Add(dictionary);
        repository.Add(otherDictionary);
        repository.Add(orders);
        repository.Add(invoices);

        var uses = repository.GetDictionaryUses(dictionary.Id);

        Assert.That(uses.Select(use => (use.Tile.Id, use.Tile.Name, TileLabels.Normalize(use.ValueLabel))), Is.EquivalentTo(new[]
        {
            (orders.Id, "Zamówienia", "tłumaczenie"),
            (orders.Id, "Zamówienia", "waga"),
            (invoices.Id, "Faktury", "tłumaczenie")
        }));
        Assert.That(repository.GetDictionaryUses(invoices.Id), Is.Empty);
    }

    private TileDefinition Definition(string name, IReadOnlyList<TileField> fields)
        => new TileDefinition(
            name,
            TileDisplays.Details,
            fields
                .Select(field => field.Source)
                .Where(source => source is not null)
                .Distinct()
                .Select(source => new TileSource(
                    source,
                    TileSourceKinds.Connection,
                    source == "customer" ? customerConnectionId : rateConnectionId))
                .ToList(),
            null,
            fields);

    [Test]
    public void Given_TileWithCombinedFields_When_ItIsSavedAndUpdated_Then_CombinationsRoundTrip()
    {
        var tile = Tile.Restore(Guid.NewGuid(), Definition("Adres",
        [
            new TileField("customer", "$.street", "Ulica", [], Hidden: true),
            new TileField("customer", "$.city", "Miasto", [], Hidden: true),
            new TileField(null, null, "Miejsce", [new TransformationStep("uppercase")])
            {
                Combination = new TileFieldCombination("join", ", ", ["Ulica", "Miasto"])
            },
            new TileField(null, null, "Adres", []) { Combination = new TileFieldCombination("template", "{Ulica}, {Miasto}", []) }
        ]));

        repository.Add(tile);

        Assert.That(Json(repository.Get(tile.Id)), Is.EqualTo(Json(tile)));

        var updated = Tile.Restore(tile.Id, tile.Definition with
        {
            Fields =
            [
                tile.Definition.Fields[0],
                new TileField(null, null, "Punkt", []) { Combination = new TileFieldCombination("object", null, ["Ulica"]) }
            ]
        });
        repository.Update(updated);

        Assert.That(Json(repository.Get(tile.Id)), Is.EqualTo(Json(updated)));
    }

    private static string Json(Tile tile) => JsonSerializer.Serialize(tile);

    private Guid AddConnection(string name)
    {
        var connection = Connection.Restore(Guid.NewGuid(), new GetConnectionDefinition(name, "https://api.example.com", [], []));
        new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Add(connection);
        return connection.Id;
    }
}
