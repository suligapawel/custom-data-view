using CustomDataView.Domain.Entities;
using CustomDataView.Domain.Repositories;

namespace CustomDataView.WebApi.Tests;

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
            .Where(tile => tile.Definition.Sources.Any(source => source.ConnectionId == connectionId))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();

    public IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids)
        => tiles
            .Where(tile => ids.Contains(tile.Id))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();

    public IReadOnlyList<TileReference> GetParents(Guid tileId)
        => tiles
            .Where(tile => tile.Definition.Children.Any(child => child.TileId == tileId))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();

    public IReadOnlyList<TileReference> GetActionReferences(Guid tileId)
        => tiles
            .Where(tile => tile.Id != tileId && tile.Definition.Actions.Any(action => action.TileId == tileId))
            .Select(tile => new TileReference(tile.Id, tile.Definition.Name))
            .ToList();

    public IReadOnlyList<TileFieldUse> GetFieldUses(Guid tileId)
        => tiles
            .SelectMany(tile => tile.Definition.Bindings
                .Where(binding => Resolve(tile, binding.From) == tileId)
                .SelectMany(binding => binding.Map)
                .Select(mapping => new TileFieldUse(new TileReference(tile.Id, tile.Definition.Name), mapping.Field)))
            .Distinct()
            .ToList();

    public IReadOnlyList<TileDictionaryUse> GetDictionaryUses(Guid tileId)
        => tiles
            .SelectMany(tile => tile.Definition.DictionaryReferences()
                .Where(reference => reference.TileId == tileId)
                .Distinct()
                .Select(reference => new TileDictionaryUse(new TileReference(tile.Id, tile.Definition.Name), reference.ValueLabel)))
            .ToList();

    private Guid? Resolve(Tile tile, string path)
    {
        Guid? resolved = null;
        foreach (var key in path.Split('/'))
        {
            var child = tile?.Definition.Children.FirstOrDefault(candidate => candidate.Key == key);
            if (child is null)
            {
                return null;
            }

            resolved = child.TileId;
            tile = Get(child.TileId);
        }

        return resolved;
    }

    public void Clear()
    {
        tiles.Clear();
    }
}
