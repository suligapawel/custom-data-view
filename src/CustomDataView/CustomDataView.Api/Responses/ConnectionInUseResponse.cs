using CustomDataView.Core.Tiles.Persistence;

namespace CustomDataView.Api.Responses;

public sealed record ConnectionInUseResponse(string Code, IReadOnlyList<TileReference> Tiles)
{
    public const string ConnectionInUseCode = "connection_in_use";
}
