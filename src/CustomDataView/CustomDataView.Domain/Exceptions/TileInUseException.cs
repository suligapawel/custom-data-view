using CustomDataView.Domain.Repositories;

namespace CustomDataView.Domain.Exceptions;

public sealed class TileInUseException(Guid tileId, IReadOnlyList<ViewReference> views, IReadOnlyList<TileReference> tiles)
    : Exception($"Tile {tileId} is used by views or tiles.")
{
    public Guid TileId { get; } = tileId;

    public IReadOnlyList<ViewReference> Views { get; } = views;

    public IReadOnlyList<TileReference> Tiles { get; } = tiles;
}
