using CustomDataView.Domain.Repositories;

namespace CustomDataView.Domain.Exceptions;

public sealed class TileInUseException(Guid tileId, IReadOnlyList<TileReference> tiles)
    : Exception($"Tile {tileId} is embedded in other tiles.")
{
    public Guid TileId { get; } = tileId;

    public IReadOnlyList<TileReference> Tiles { get; } = tiles;
}
