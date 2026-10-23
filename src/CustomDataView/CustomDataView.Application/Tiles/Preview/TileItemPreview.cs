using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TileItemPreview(JsonNode Key, IReadOnlyList<TileFieldPreview> Fields)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TileEmbeddingPreview> Children { get; init; }
}
