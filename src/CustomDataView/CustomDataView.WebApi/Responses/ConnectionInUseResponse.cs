using CustomDataView.Domain.Repositories;

namespace CustomDataView.WebApi.Responses;

public sealed record ConnectionInUseResponse(string Code, IReadOnlyList<TileReference> Tiles)
{
    public const string ConnectionInUseCode = "connection_in_use";
}
