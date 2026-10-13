using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CustomDataView.Core.Tiles.Preview;

public sealed record TileFieldPreview(
    string Label,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonNode Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileFieldError Error = null);
