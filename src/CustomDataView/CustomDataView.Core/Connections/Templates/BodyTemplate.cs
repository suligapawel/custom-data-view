using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Templates;

public static partial class BodyTemplate
{
    public static string Resolve(
        string bodyTemplate,
        IReadOnlyList<ParameterDeclaration> bodyParameters,
        IReadOnlyDictionary<string, JsonNode> bodyValues)
    {
        var body = JsonNode.Parse(bodyTemplate);
        return Resolve(body, bodyValues)?.ToJsonString() ?? "null";
    }

    public static IReadOnlyList<string> PlaceholderNames(JsonElement body)
    {
        var names = new List<string>();
        Collect(body, names);
        return names.Distinct().ToList();
    }

    private static JsonNode Resolve(JsonNode node, IReadOnlyDictionary<string, JsonNode> bodyValues)
    {
        if (TryGetPlaceholder(node, out var name))
        {
            return bodyValues.TryGetValue(name, out var value) ? value?.DeepClone() : null;
        }

        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var key in jsonObject.Select(property => property.Key).ToList())
                {
                    ReplaceChild(jsonObject[key], replacement => jsonObject[key] = replacement, bodyValues);
                }

                break;
            case JsonArray jsonArray:
                for (var index = 0; index < jsonArray.Count; index++)
                {
                    var position = index;
                    ReplaceChild(jsonArray[index], replacement => jsonArray[position] = replacement, bodyValues);
                }

                break;
        }

        return node;
    }

    private static void ReplaceChild(
        JsonNode child,
        Action<JsonNode> replace,
        IReadOnlyDictionary<string, JsonNode> bodyValues)
    {
        if (TryGetPlaceholder(child, out _))
        {
            replace(Resolve(child, bodyValues));
        }
        else
        {
            Resolve(child, bodyValues);
        }
    }

    private static bool TryGetPlaceholder(JsonNode node, out string name)
    {
        name = null;
        if (node is JsonValue value
            && value.TryGetValue<string>(out var text)
            && PlaceholderPattern().Match(text) is { Success: true } match)
        {
            name = match.Groups["name"].Value;
            return true;
        }

        return false;
    }

    private static void Collect(JsonElement element, List<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Collect(property.Value, names);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, names);
                }

                break;
            case JsonValueKind.String:
                var match = PlaceholderPattern().Match(element.GetString()!);
                if (match.Success)
                {
                    names.Add(match.Groups["name"].Value);
                }

                break;
        }
    }

    [GeneratedRegex("^\\{(?<name>[a-z][a-z0-9_]{0,49})\\}$")]
    private static partial Regex PlaceholderPattern();
}
