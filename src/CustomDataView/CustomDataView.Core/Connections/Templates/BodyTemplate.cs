using System.Text.Json;
using System.Text.RegularExpressions;

namespace CustomDataView.Core.Connections.Templates;

public static partial class BodyTemplate
{
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
