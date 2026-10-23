using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileFormFieldPreview(string Type, bool Required, JsonNode Default);
