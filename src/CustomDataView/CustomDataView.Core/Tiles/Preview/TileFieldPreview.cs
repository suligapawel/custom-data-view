using System.Text.Json.Nodes;

namespace CustomDataView.Core.Tiles.Preview;

public sealed record TileFieldPreview(string Label, JsonNode Value);
