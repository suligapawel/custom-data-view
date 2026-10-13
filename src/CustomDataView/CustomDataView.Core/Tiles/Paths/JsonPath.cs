using System.Text.Json.Nodes;

namespace CustomDataView.Core.Tiles.Paths;

public sealed record JsonPath(IReadOnlyList<JsonPathSegment> Segments)
{
    public bool HasWildcard => Segments.Any(segment => segment is JsonPathSegment.Wildcard);

    public IReadOnlyList<JsonNode> Evaluate(JsonNode root)
    {
        IEnumerable<JsonNode> matches = [root];
        foreach (var segment in Segments)
        {
            matches = matches.SelectMany(node => Match(node, segment)).ToList();
        }

        return matches.ToList();
    }

    private static IEnumerable<JsonNode> Match(JsonNode node, JsonPathSegment segment)
        => segment switch
        {
            JsonPathSegment.Property property when node is JsonObject json && json.TryGetPropertyValue(property.Name, out var value)
                => new List<JsonNode> { value },
            JsonPathSegment.Wildcard when node is JsonArray array => array.ToList(),
            _ => []
        };
}
