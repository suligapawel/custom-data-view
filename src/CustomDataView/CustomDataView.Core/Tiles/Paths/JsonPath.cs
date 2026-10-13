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
            JsonPathSegment.Index index when node is JsonArray array && ElementAt(array, index.Value) is var position
                && position >= 0 && position < array.Count
                => new List<JsonNode> { array[position] },
            JsonPathSegment.Wildcard when node is JsonArray array => array.ToList(),
            JsonPathSegment.Wildcard when node is JsonObject json => json.Select(property => property.Value).ToList(),
            _ => []
        };

    private static int ElementAt(JsonArray array, int index) => index < 0 ? array.Count + index : index;
}
