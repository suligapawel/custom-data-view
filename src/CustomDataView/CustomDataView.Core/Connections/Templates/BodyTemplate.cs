using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Definitions;
using System.Text.RegularExpressions;

namespace CustomDataView.Core.Connections.Templates;

public static partial class BodyTemplate
{
    public static string Resolve(
        string bodyTemplate,
        IReadOnlyList<ParameterDeclaration> bodyParameters,
        IReadOnlyDictionary<string, JsonNode> bodyValues)
    {
        var body = JsonNode.Parse(bodyTemplate);
        return Replace(body, bodyParameters, bodyValues)?.ToJsonString();
    }

    private static JsonNode Replace(
        JsonNode node,
        IReadOnlyList<ParameterDeclaration> bodyParameters,
        IReadOnlyDictionary<string, JsonNode> bodyValues)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    jsonObject[property.Key] = Replace(property.Value, bodyParameters, bodyValues);
                }

                return jsonObject;
            case JsonArray jsonArray:
                for (var index = 0; index < jsonArray.Count; index++)
                {
                    jsonArray[index] = Replace(jsonArray[index], bodyParameters, bodyValues);
                }

                return jsonArray;
            case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text)
                && PlaceholderPattern().Match(text) is { Success: true } match:
                var name = match.Groups["name"].Value;
                return bodyValues.TryGetValue(name, out var value) ? value?.DeepClone() : null;
            default:
                return node?.DeepClone();
        }
    }

    public static IReadOnlyList<string> PlaceholderNames(JsonElement body)
    {
        var names = new List<string>();
        Collect(body, names);
        return names.Distinct().ToList();
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
