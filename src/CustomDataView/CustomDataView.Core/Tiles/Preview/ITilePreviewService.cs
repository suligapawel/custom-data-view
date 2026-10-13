using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Preview;

public interface ITilePreviewService
{
    Task<TilePreview> Preview(Guid id, IReadOnlyList<ConnectionValuesInput> connections, CancellationToken cancellationToken);

    Task<TilePreview> Preview(
        TileDefinition definition,
        IReadOnlyList<ConnectionValuesInput> connections,
        CancellationToken cancellationToken);
}
