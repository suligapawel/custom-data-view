using System.Text.Json;
using CustomDataView.Core.Tiles.Definitions;

namespace CustomDataView.Core.Tiles.Preview;

public interface ITilePreviewService
{
    Task<TilePreview> Preview(
        Guid id,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken);

    Task<TilePreview> Preview(
        TileDefinition definition,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken);
}
