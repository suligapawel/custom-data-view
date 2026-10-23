using CustomDataView.Application.Tiles.Preview;

namespace CustomDataView.Application.Views.Preview;

public sealed record ViewTilePreview(
    Guid TileId,
    string TileName,
    int Position,
    string CssClass,
    IReadOnlyList<TileFieldPreview> Fields);
