using CustomDataView.Core.Tiles.Persistence;

namespace CustomDataView.Core.Exceptions;

public sealed class ConnectionInUseException(Guid connectionId, IReadOnlyList<TileReference> tiles)
    : Exception($"Connection {connectionId} is used by tiles.")
{
    public Guid ConnectionId { get; } = connectionId;

    public IReadOnlyList<TileReference> Tiles { get; } = tiles;
}
