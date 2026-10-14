using System.Text.Json.Serialization;
using CustomDataView.Core.Tiles.Preview;

namespace CustomDataView.Core.Views.Preview;

public sealed record ViewTilePreview(
    Guid TileId,
    string TileName,
    int Position,
    string CssClass,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TileFieldPreview> Fields,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ViewTileError Error = null);
