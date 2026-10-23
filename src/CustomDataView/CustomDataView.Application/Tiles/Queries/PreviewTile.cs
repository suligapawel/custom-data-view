using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Tiles.Preview;

namespace CustomDataView.Application.Tiles.Queries;

public sealed record PreviewTile(Guid Id, TilePreviewValues Values) : IQuery<TilePreview>;

public sealed class PreviewTileHandler(TilePreviewer previewer)
{
    public Task<TilePreview> Handle(PreviewTile query, CancellationToken cancellationToken)
        => previewer.Preview(query.Id, query.Values, cancellationToken);
}
