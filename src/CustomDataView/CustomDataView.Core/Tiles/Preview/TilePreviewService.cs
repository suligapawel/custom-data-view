using CustomDataView.Core.Tiles.Persistence;

namespace CustomDataView.Core.Tiles.Preview;

public sealed class TilePreviewService(ITileRepository tiles) : ITilePreviewService
{
    public Task<TilePreview> Preview(Guid id, CancellationToken cancellationToken)
    {
        var tile = tiles.Get(id);
        return Task.FromResult(new TilePreview(tile.Id, tile.Definition.Name));
    }
}
