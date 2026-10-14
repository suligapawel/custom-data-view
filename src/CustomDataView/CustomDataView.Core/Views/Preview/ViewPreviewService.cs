using System.Text.Json;
using CustomDataView.Core.Exceptions;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Tiles.Preview;
using CustomDataView.Core.Views.Definitions;
using CustomDataView.Core.Views.Persistence;

namespace CustomDataView.Core.Views.Preview;

public sealed class ViewPreviewService(IViewRepository views, ITileRepository tiles, ITilePreviewService tilePreviews)
    : IViewPreviewService
{
    public async Task<ViewPreview> Preview(
        Guid id,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        var view = views.Get(id) ?? throw new NotFoundException(id);
        var names = tiles.GetReferences(view.Definition.Tiles.Select(tile => tile.TileId).ToList())
            .ToDictionary(tile => tile.Id, tile => tile.Name);
        var previews = await Task.WhenAll(view.Definition.Tiles
            .OrderBy(tile => tile.Position)
            .Select(tile => Preview(tile, names[tile.TileId], parameters, cancellationToken)));
        return new ViewPreview(view.Id, view.Definition.Name, previews);
    }

    private async Task<ViewTilePreview> Preview(
        ViewTile tile,
        string name,
        IReadOnlyDictionary<string, JsonElement> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await tilePreviews.Preview(tile.TileId, parameters, cancellationToken);
            return new ViewTilePreview(tile.TileId, name, tile.Position, tile.CssClass, preview.Fields);
        }
        catch (ValidationException exception)
        {
            return new ViewTilePreview(
                tile.TileId,
                name,
                tile.Position,
                tile.CssClass,
                null,
                new ViewTileError(ViewTileErrorCodes.InvalidParameters, exception.Errors));
        }
    }
}
