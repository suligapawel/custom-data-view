using System.Text.Json;
using CustomDataView.Core.Exceptions;
using CustomDataView.Core.Tiles.Preview;
using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Persistence;

namespace CustomDataView.Core.Views.Preview;

public sealed class ViewPreviewService(IViewRepository views, ITilePreviewService tiles) : IViewPreviewService
{
    public async Task<ViewPreview> Preview(
        Guid id,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var view = views.Get(id);
        var previews = await Task.WhenAll(view.Definition.Tiles
            .OrderBy(tile => tile.Position)
            .Select(tile => Preview(tile, parameters, cancellationToken)));
        return new ViewPreview(view.Id, view.Definition.Name, previews);
    }

    private async Task<ViewTilePreview> Preview(
        ViewTile tile,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await tiles.Preview(tile.TileId, parameters, cancellationToken);
            return new ViewTilePreview(tile.TileId, preview.Name, tile.Position, tile.CssClass, preview.Fields);
        }
        catch (ValidationException exception)
        {
            return new ViewTilePreview(
                tile.TileId,
                null,
                tile.Position,
                tile.CssClass,
                null,
                new ViewTileError(ViewTileErrorCodes.InvalidParameters, exception.Errors));
        }
    }
}
