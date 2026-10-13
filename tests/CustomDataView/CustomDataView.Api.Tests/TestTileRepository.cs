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
}
