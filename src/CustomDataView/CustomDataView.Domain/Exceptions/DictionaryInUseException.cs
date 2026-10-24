using CustomDataView.Domain.Repositories;

namespace CustomDataView.Domain.Exceptions;

public sealed class DictionaryInUseException(Guid tileId, IReadOnlyList<string> fields, IReadOnlyList<TileReference> tiles)
    : Exception($"Tile {tileId} is used as a dictionary.")
{
    public Guid TileId { get; } = tileId;

    public IReadOnlyList<string> Fields { get; } = fields;

    public IReadOnlyList<TileReference> Tiles { get; } = tiles;
}
