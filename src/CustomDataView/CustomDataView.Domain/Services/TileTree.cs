using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public sealed class TileTree(Func<Guid, TileDefinition> tileOf)
{
    public TileDefinition Tile(Guid id) => tileOf(id);

    public bool Embeds(TileDefinition tile, Guid id) => Embeds(tile, id, []);

    public TileDefinition SourceTile(TileChild child, IReadOnlyList<string> path)
    {
        if (path.Count > 1 && child.Scope != TileChildScopes.Tile)
        {
            return null;
        }

        var tile = tileOf(child.TileId);
        foreach (var key in path.Skip(1))
        {
            var descendant = tile?.Child(key);
            if (descendant is null || descendant.Scope != TileChildScopes.Tile)
            {
                return null;
            }

            tile = tileOf(descendant.TileId);
        }

        return tile;
    }

    public TileTreeTarget Target(TileChild child, IReadOnlyList<string> path)
    {
        var tile = tileOf(child.TileId);
        var intermediates = new List<TileDefinition>();
        foreach (var key in path.Skip(1))
        {
            var descendant = tile?.Child(key);
            if (descendant is null)
            {
                return null;
            }

            intermediates.Add(tile);
            child = descendant;
            tile = tileOf(descendant.TileId);
        }

        return tile is null ? null : new TileTreeTarget(child, tile, intermediates);
    }

    private bool Embeds(TileDefinition tile, Guid id, HashSet<Guid> visited)
        => tile.Children.Any(child => child.TileId == id
            || (visited.Add(child.TileId) && tileOf(child.TileId) is { } descendant && Embeds(descendant, id, visited)));
}

public sealed record TileTreeTarget(TileChild Child, TileDefinition Tile, IReadOnlyList<TileDefinition> Intermediates);
