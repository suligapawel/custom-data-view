using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Entities;
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
        repository = new PostgresTileRepository(TestDatabase.ContextFactory(dataSource), [new RoundTransformation(), new SuffixTransformation()]);
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
        var first = new Tile(Guid.NewGuid(), new TileDefinition("Klient", []));
        var second = new Tile(Guid.NewGuid(), new TileDefinition("Kurs", []));

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
        var tile = new Tile(Guid.NewGuid(), new TileDefinition("Klient i kurs",
        [
            new TileField(rateConnectionId, "$.rate", "Kurs EUR", []),
            new TileField(customerConnectionId, "$.name", "Nazwa", []),
            new TileField(customerConnectionId, "$['unit-name']", "Dział", [], Hidden: true)
        ]));

        repository.Add(tile);

        var loaded = repository.Get(tile.Id);
        Assert.That(Json(loaded), Is.EqualTo(Json(tile)));
    }

    [Test]
    public void Given_FieldWithTransformationChain_When_TileIsLoaded_Then_KeysOrderAndTypedParametersAreUnchanged()
    {
        var tile = new Tile(Guid.NewGuid(), new TileDefinition("Klient",
        [
            new TileField(customerConnectionId, "$.balance", "Saldo",
            [
                new TransformationStep("round", new Dictionary<string, JsonNode> { ["decimals"] = 2 }),
                new TransformationStep("suffix", new Dictionary<string, JsonNode> { ["text"] = " zł" }),
                new TransformationStep("suffix", new Dictionary<string, JsonNode> { ["text"] = "2" }),
                new TransformationStep("round", new Dictionary<string, JsonNode> { ["decimals"] = "dwa", ["extra"] = 1.5m })
            ])
        ]));

        repository.Add(tile);

        Assert.That(Json(repository.Get(tile.Id)), Is.EqualTo(Json(tile with { Definition = tile.Definition with
        {
            Fields =
            [
                tile.Definition.Fields[0] with
                {
                    Transformations =
                    [
                        .. tile.Definition.Fields[0].Transformations.Take(3),
                        new TransformationStep("round", new Dictionary<string, JsonNode> { ["decimals"] = "dwa", ["extra"] = "1.5" })
                    ]
                }
            ]
        } })));
    }

    [Test]
    public void Given_TwoSavedTiles_When_FirstIsUpdated_Then_ItsDefinitionIsReplacedAndOrderIsKept()
    {
        var first = new Tile(Guid.NewGuid(), new TileDefinition("Klient",
        [
            new TileField(customerConnectionId, "$.name", "Nazwa",
                [new TransformationStep("round", new Dictionary<string, JsonNode> { ["decimals"] = 2 })]),
            new TileField(customerConnectionId, "$.balance", "Saldo", [])
        ]));
        var second = new Tile(Guid.NewGuid(), new TileDefinition("Kurs", [new TileField(rateConnectionId, "$.rate", "Kurs EUR", [])]));
        repository.Add(first);
        repository.Add(second);
        var updated = first with
        {
            Definition = new TileDefinition("Klient VIP",
            [
                new TileField(rateConnectionId, "$.address.city", "Miasto",
                    [new TransformationStep("suffix", new Dictionary<string, JsonNode> { ["text"] = " zł" })])
            ])
        };

        repository.Update(updated);

        Assert.That(repository.GetAll().Select(Json), Is.EqualTo(new[] { Json(updated), Json(second) }));
    }

    [Test]
    public void Given_SavedTileWithTransformations_When_ItIsDeleted_Then_ItIsGoneAndConnectionsRemain()
    {
        var tile = new Tile(Guid.NewGuid(), new TileDefinition("Klient",
        [
            new TileField(customerConnectionId, "$.balance", "Saldo",
                [new TransformationStep("round", new Dictionary<string, JsonNode> { ["decimals"] = 2 })])
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
        var customer = new Tile(Guid.NewGuid(), new TileDefinition("Klient",
        [
            new TileField(customerConnectionId, "$.name", "Nazwa", []),
            new TileField(customerConnectionId, "$.balance", "Saldo", [])
        ]));
        var rate = new Tile(Guid.NewGuid(), new TileDefinition("Kurs", [new TileField(rateConnectionId, "$.rate", "Kurs EUR", [])]));
        var both = new Tile(Guid.NewGuid(), new TileDefinition("Klient i kurs",
        [
            new TileField(rateConnectionId, "$.rate", "Kurs EUR", []),
            new TileField(customerConnectionId, "$.name", "Nazwa", [])
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
        repository.Add(new Tile(Guid.NewGuid(), new TileDefinition("Klient", [new TileField(customerConnectionId, "$.name", "Nazwa", [])])));

        Assert.Throws<PostgresException>(() => new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Delete(customerConnectionId));
    }

    [Test]
    public void Given_TileWithLabelsDifferingInCaseAndSpaces_When_ItIsSaved_Then_DatabaseRejectsIt()
    {
        var tile = new Tile(Guid.NewGuid(), new TileDefinition("Klient",
        [
            new TileField(customerConnectionId, "$.name", "Nazwa", []),
            new TileField(customerConnectionId, "$.balance", " NAZWA ", [])
        ]));

        Assert.That(() => repository.Add(tile), Throws.InstanceOf<Microsoft.EntityFrameworkCore.DbUpdateException>());
        Assert.That(repository.Get(tile.Id), Is.Null);
    }

    private static string Json(Tile tile) => JsonSerializer.Serialize(tile);

    private Guid AddConnection(string name)
    {
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition(name, "https://api.example.com", [], []));
        new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Add(connection);
        return connection.Id;
    }
}
