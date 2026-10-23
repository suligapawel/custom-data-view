using System.Text.Json;
using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Application.Views.Preview;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Views.Queries;

public sealed record PreviewView(Guid Id, IReadOnlyDictionary<string, JsonElement> Parameters) : IQuery<ViewPreview>;

public sealed class PreviewViewHandler(IViewRepository views, ITileRepository tiles, TilePreviewer tilePreviewer)
{
    public async Task<ViewPreview> Handle(PreviewView query, CancellationToken cancellationToken)
    {
        var view = views.Get(query.Id) ?? throw new NotFoundException(query.Id);
        var names = tiles.GetReferences(view.Definition.Tiles.Select(tile => tile.TileId).ToList())
            .ToDictionary(tile => tile.Id, tile => tile.Name);
        var previews = await Task.WhenAll(view.Definition.Tiles
            .OrderBy(tile => tile.Position)
            .Select(tile => Preview(tile, names[tile.TileId], query.Parameters, cancellationToken)));
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
            var preview = await tilePreviewer.Preview(tile.TileId, parameters, cancellationToken);
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
