using CustomDataView.Domain.Repositories;

namespace CustomDataView.Domain.Exceptions;

public sealed class FieldInUseException(Guid tileId, IReadOnlyList<string> fields, IReadOnlyList<TileReference> tiles)
    : Exception($"Fields of tile {tileId} are used by bindings.")
{
    public Guid TileId { get; } = tileId;

    public IReadOnlyList<string> Fields { get; } = fields;

    public IReadOnlyList<TileReference> Tiles { get; } = tiles;
}
