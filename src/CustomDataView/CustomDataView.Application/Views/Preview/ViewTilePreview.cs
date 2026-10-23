using System.Text.Json.Serialization;
using CustomDataView.Application.Tiles.Preview;

namespace CustomDataView.Application.Views.Preview;

public sealed record ViewTilePreview(
    Guid TileId,
    string TileName,
    int Position,
    string CssClass,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TileFieldPreview> Fields,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ViewTileError Error = null);
