using System.Text.Json;
using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;
using CustomDataView.Infrastructure.Persistence.Repositories;
using Npgsql;

namespace CustomDataView.Infrastructure.Tests;

public class PostgresViewRepositoryTests
{
    private NpgsqlDataSource dataSource;
    private PostgresViewRepository repository;
    private PostgresTileRepository tiles;
    private Guid customerTileId;
    private Guid rateTileId;

    [OneTimeSetUp]
    public void CreateDatabase()
    {
        dataSource = TestDatabase.Create();
        repository = new PostgresViewRepository(TestDatabase.ContextFactory(dataSource));
        tiles = new PostgresTileRepository(TestDatabase.ContextFactory(dataSource), []);
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
        var connection = new Connection(Guid.NewGuid(), new GetConnectionDefinition("Klient", "https://api.example.com", [], []));
        new PostgresConnectionRepository(TestDatabase.ContextFactory(dataSource)).Add(connection);
        customerTileId = AddTile("Klient", connection.Id);
        rateTileId = AddTile("Kurs walut", connection.Id);
    }

    [Test]
    public void Given_SavedViewWithTiles_When_ItIsLoaded_Then_TilesAreInPositionOrderWithCssClasses()
    {
        var view = new View(Guid.NewGuid(), new ViewDefinition("Pulpit",
        [
            new ViewTile(rateTileId, 5, null),
            new ViewTile(customerTileId, 0, "col-6 highlight")
        ]));

        repository.Add(view);

        Assert.That(Json(repository.Get(view.Id)), Is.EqualTo(Json(view with
        {
            Definition = view.Definition with { Tiles = [view.Definition.Tiles[1], view.Definition.Tiles[0]] }
        })));
    }

    [Test]
    public void Given_TwoSavedViews_When_AllAreLoaded_Then_TheyAreInAddingOrder()
    {
        var first = new View(Guid.NewGuid(), new ViewDefinition("Pulpit", []));
        var second = new View(Guid.NewGuid(), new ViewDefinition("Pulpit",
            [new ViewTile(customerTileId, 0, null), new ViewTile(rateTileId, 1, null)]));

        repository.Add(first);
        repository.Add(second);

        Assert.That(repository.GetAll(), Is.EqualTo(new[] { new ViewSummary(first.Id, "Pulpit", 0), new ViewSummary(second.Id, "Pulpit", 2) }));
    }

    [Test]
    public void Given_ViewWithTile_When_TileIsUpdated_Then_ViewKeepsItsPositionAndCssClass()
    {
        var view = new View(Guid.NewGuid(), new ViewDefinition("Pulpit", [new ViewTile(customerTileId, 3, "col-6")]));
        repository.Add(view);
        var tile = tiles.Get(customerTileId);

        tiles.Update(tile with { Definition = tile.Definition with { Name = "Klient VIP" } });

        Assert.That(Json(repository.Get(view.Id)), Is.EqualTo(Json(view)));
    }

    [Test]
    public void Given_TwoViewsWithTile_When_TileIsDeleted_Then_ItIsRemovedFromBothViewsWhichRemain()
    {
        var sales = new View(Guid.NewGuid(), new ViewDefinition("Sprzedaż",
            [new ViewTile(customerTileId, 0, null), new ViewTile(rateTileId, 1, null)]));
        var service = new View(Guid.NewGuid(), new ViewDefinition("Obsługa", [new ViewTile(customerTileId, 0, null)]));
        repository.Add(sales);
        repository.Add(service);

        tiles.Delete(customerTileId);

        Assert.That(repository.Get(sales.Id).Definition.Tiles.Select(tile => tile.TileId), Is.EqualTo(new[] { rateTileId }));
        Assert.That(repository.Get(service.Id).Definition.Tiles, Is.Empty);
    }

    [Test]
    public void Given_TwoSavedViews_When_FirstIsUpdated_Then_NameAndTilesAreReplacedAndOrderIsKept()
    {
        var first = new View(Guid.NewGuid(), new ViewDefinition("Pulpit",
            [new ViewTile(customerTileId, 0, null), new ViewTile(rateTileId, 1, null)]));
        var second = new View(Guid.NewGuid(), new ViewDefinition("Obsługa", []));
        repository.Add(first);
        repository.Add(second);
        var updated = first with { Definition = new ViewDefinition("Pulpit klienta", [new ViewTile(rateTileId, 0, "col-12")]) };

        repository.Update(updated);

        Assert.That(Json(repository.Get(first.Id)), Is.EqualTo(Json(updated)));
        Assert.That(repository.GetAll().Select(view => view.Id), Is.EqualTo(new[] { first.Id, second.Id }));
    }

    [Test]
    public void Given_SavedViewWithTile_When_ItIsDeleted_Then_ItIsGoneAndTileRemains()
    {
        var view = new View(Guid.NewGuid(), new ViewDefinition("Pulpit", [new ViewTile(customerTileId, 0, null)]));
        repository.Add(view);

        repository.Delete(view.Id);

        Assert.That(repository.Get(view.Id), Is.Null);
        Assert.That(repository.GetAll(), Is.Empty);
        Assert.That(tiles.Get(customerTileId), Is.Not.Null);
    }

    [Test]
    public void Given_ViewDoesNotExist_When_ItIsLoaded_Then_NothingIsReturned()
    {
        Assert.That(repository.Get(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void Given_SavedTiles_When_ReferencesAreLoaded_Then_OnlyExistingTilesAreReturned()
    {
        Assert.That(
            tiles.GetReferences([customerTileId, Guid.NewGuid()]),
            Is.EqualTo(new[] { new TileReference(customerTileId, "Klient") }));
        Assert.That(tiles.GetReferences([]), Is.Empty);
    }

    private static string Json(View view) => JsonSerializer.Serialize(view);

    private Guid AddTile(string name, Guid connectionId)
    {
        var tile = new Tile(Guid.NewGuid(), new TileDefinition(name, [new TileField(connectionId, "$.name", "Nazwa", [])]));
        tiles.Add(tile);
        return tile.Id;
    }
}
