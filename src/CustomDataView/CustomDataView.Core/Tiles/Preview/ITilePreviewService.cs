namespace CustomDataView.Core.Tiles.Preview;

public interface ITilePreviewService
{
    Task<TilePreview> Preview(Guid id, IReadOnlyList<ConnectionValuesInput> connections, CancellationToken cancellationToken);
}
