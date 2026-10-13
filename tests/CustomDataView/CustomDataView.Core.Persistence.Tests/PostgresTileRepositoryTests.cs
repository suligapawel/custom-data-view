using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;
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
        repository = new PostgresTileRepository(dataSource);
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
            new TileField(rateConnectionId, "$.rate", "Kurs EUR"),
            new TileField(customerConnectionId, "$.name", "Nazwa"),
            new TileField(customerConnectionId, "$['unit-name']", "Nazwa")
        ]));

        repository.Add(tile);

        var loaded = repository.Get(tile.Id);
        Assert.That(loaded.Definition.Name, Is.EqualTo("Klient i kurs"));
        Assert.That(loaded.Definition.Fields, Is.EqualTo(tile.Definition.Fields));
    }

    private Guid AddConnection(string name)
    {
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition(name, "https://api.example.com", [], []));
        new PostgresConnectionRepository(dataSource).Add(connection);
        return connection.Id;
    }
}
