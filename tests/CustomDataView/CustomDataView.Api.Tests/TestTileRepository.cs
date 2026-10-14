using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Persistence;

namespace CustomDataView.Api.Tests;

public sealed class TestTileRepository : ITileRepository
{
    private readonly List<Tile> tiles = [];

    public void Add(Tile tile)
    {
        tiles.Add(tile);
    }

    public Tile Get(Guid id) => tiles.SingleOrDefault(tile => tile.Id == id);

    public IReadOnlyList<Tile> GetAll() => tiles.ToList();

    public void Update(Tile tile)
    {
        var index = tiles.FindIndex(stored => stored.Id == tile.Id);
        tiles[index] = tile;
    }

    public void Delete(Guid id)
    {
        tiles.RemoveAll(tile => tile.Id == id);
    }

    public IReadOnlyList<TileReference> GetUsingConnection(Guid connectionId)
        => tiles
            .Where(tile => tile.Definition.Fields.Any(field => field.ConnectionId == connectionId))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();

    public IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids)
        => tiles
            .Where(tile => ids.Contains(tile.Id))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();
}
