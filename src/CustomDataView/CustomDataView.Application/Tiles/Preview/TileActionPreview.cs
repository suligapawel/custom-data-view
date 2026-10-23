using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileActionPreview(
    string Key,
    string Label,
    string Type,
    Guid TileId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileValues Inputs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileFieldError Error = null);
