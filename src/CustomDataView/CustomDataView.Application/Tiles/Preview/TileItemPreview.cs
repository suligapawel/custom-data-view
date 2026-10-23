using System.Text.Json.Nodes;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileItemPreview(JsonNode Key, IReadOnlyList<TileFieldPreview> Fields);
