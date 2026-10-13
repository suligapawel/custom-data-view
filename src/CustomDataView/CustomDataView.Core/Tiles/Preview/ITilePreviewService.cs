namespace CustomDataView.Core.Tiles.Preview;

public interface ITilePreviewService
{
    Task<TilePreview> Preview(Guid id, CancellationToken cancellationToken);
}
