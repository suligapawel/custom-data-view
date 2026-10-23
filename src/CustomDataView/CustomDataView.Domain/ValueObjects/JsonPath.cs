using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public sealed record JsonPath(IReadOnlyList<JsonPathSegment> Segments)
{
    public bool IsCollection => Segments.Any(segment => segment is JsonPathSegment.Wildcard or JsonPathSegment.Filter);

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
            JsonPathSegment.Wildcard => Children(node),
            JsonPathSegment.Filter filter => Children(node).Where(child => Satisfies(child, filter)).ToList(),
            _ => []
        };

    private static List<JsonNode> Children(JsonNode node)
        => node switch
        {
            JsonArray array => array.ToList(),
            JsonObject json => json.Select(property => property.Value).ToList(),
            _ => []
        };

    private static bool Satisfies(JsonNode node, JsonPathSegment.Filter filter)
    {
        var operand = filter.Operand.Evaluate(node);
        var equal = operand.Count == 1 && AreEqual(operand[0], filter.Literal);
        return equal != filter.Negated;
    }

    private static bool AreEqual(JsonNode value, JsonNode literal)
    {
        if (value is null || literal is null)
        {
            return value is null && literal is null;
        }

        var kind = value.GetValueKind();
        if (kind != literal.GetValueKind())
        {
            return false;
        }

        return kind switch
        {
            JsonValueKind.Number => AreEqualNumbers(value, literal),
            JsonValueKind.String => value.GetValue<string>() == literal.GetValue<string>(),
            JsonValueKind.True or JsonValueKind.False => true,
            _ => false
        };
    }

    private static bool AreEqualNumbers(JsonNode value, JsonNode literal)
        => value.AsValue().TryGetValue<decimal>(out var left) && literal.AsValue().TryGetValue<decimal>(out var right)
            ? left == right
            : value.AsValue().TryGetValue<double>(out var leftDouble) && literal.AsValue().TryGetValue<double>(out var rightDouble)
                && leftDouble == rightDouble;

    private static int ElementAt(JsonArray array, int index) => index < 0 ? array.Count + index : index;
}
