using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;
using Npgsql;

namespace CustomDataView.Core.Persistence.Tests;

public class PostgresTileRepositoryTests
{
    private NpgsqlDataSource dataSource;
    private PostgresTileRepository repository;

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
    }

    [Test]
    public void Given_TwoSavedTiles_When_AllAreLoaded_Then_TheyAreInAddingOrder()
    {
        var first = new Tile(Guid.NewGuid(), new TileDefinition("Klient"));
        var second = new Tile(Guid.NewGuid(), new TileDefinition("Kurs"));

        repository.Add(first);
        repository.Add(second);

        Assert.That(repository.GetAll().Select(tile => (tile.Id, tile.Definition.Name)), Is.EqualTo(new[]
        {
            (first.Id, "Klient"),
            (second.Id, "Kurs")
        }));
    }
}
