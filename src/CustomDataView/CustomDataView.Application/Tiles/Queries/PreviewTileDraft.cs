using CustomDataView.Application.Abstractions;
using CustomDataView.Application.Tiles.Preview;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Queries;

public sealed record PreviewTileDraft(TileDefinition Definition, TilePreviewValues Values)
    : IQuery<TilePreview>;

public sealed class PreviewTileDraftHandler(TilePreviewer previewer)
{
    public Task<TilePreview> Handle(PreviewTileDraft query, CancellationToken cancellationToken)
        => previewer.Preview(query.Definition, query.Values, cancellationToken);
}
