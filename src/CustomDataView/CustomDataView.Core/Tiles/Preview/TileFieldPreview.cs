using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CustomDataView.Core.Tiles.Preview;

[JsonConverter(typeof(TileFieldPreviewJsonConverter))]
public sealed record TileFieldPreview(string Label, JsonNode Value, TileFieldError Error = null);
