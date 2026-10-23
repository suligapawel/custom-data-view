using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileEmbeddingPreview(
    string Key,
    string Address,
    Guid TileId,
    int Position,
    string CssClass,
    string State,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TilePreview Tile = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileValues Inputs = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TilePendingBinding> Pending = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileFieldError Error = null);
