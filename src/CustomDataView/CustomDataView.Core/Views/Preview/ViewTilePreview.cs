using CustomDataView.Core.Tiles.Preview;

namespace CustomDataView.Core.Views.Preview;

public sealed record ViewTilePreview(
    Guid TileId,
    string TileName,
    int Position,
    string CssClass,
    IReadOnlyList<TileFieldPreview> Fields);
