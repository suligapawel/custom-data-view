using CustomDataView.Domain.Repositories;

namespace CustomDataView.WebApi.Responses;

public sealed record TileInUseResponse(string Code, IReadOnlyList<ViewReference> Views, IReadOnlyList<TileReference> Tiles)
{
    public const string TileInUseCode = "tile_in_use";
}
