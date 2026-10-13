using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Transformations.Execution;
using CustomDataView.Core.Transformations.Implementations;
using Npgsql;

namespace CustomDataView.Core.Persistence.Tests;

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
        repository = new PostgresTileRepository(dataSource, [new RoundTransformation(), new SuffixTransformation()]);
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
            new TileField(customerConnectionId, "$['unit-name']", "Nazwa", [])
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
        Assert.That(new PostgresConnectionRepository(dataSource).Get(customerConnectionId), Is.Not.Null);
    }

    private static string Json(Tile tile) => JsonSerializer.Serialize(tile);

    private Guid AddConnection(string name)
    {
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition(name, "https://api.example.com", [], []));
        new PostgresConnectionRepository(dataSource).Add(connection);
        return connection.Id;
    }
}
