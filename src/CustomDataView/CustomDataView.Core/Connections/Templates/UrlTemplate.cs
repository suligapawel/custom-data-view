using System.Text.Json;
using System.Text.RegularExpressions;
using CustomDataView.Core.Connections.Definitions;

namespace CustomDataView.Core.Connections.Templates;

public static partial class UrlTemplate
{
    public static bool HasPlaceholdersOutsidePath(string url)
    {
        var queryStart = url.IndexOfAny(['?', '#']);
        return queryStart >= 0 && url.IndexOfAny(['{', '}'], queryStart) >= 0;
    }

    public static bool HasUnclosedBraces(string url)
    {
        return PlaceholderPattern().Replace(url, string.Empty).IndexOfAny(['{', '}']) >= 0;
    }

    public static Uri Resolve(
        string url,
        IReadOnlyDictionary<string, string> pathValues,
        IReadOnlyList<ParameterDeclaration> queryParameters,
        IReadOnlyDictionary<string, IReadOnlyList<string>> queryValues)
    {
        var resolved = PlaceholderPattern().Replace(url, match => Uri.EscapeDataString(pathValues[match.Value[1..^1]]));
        var query = queryParameters
            .SelectMany(parameter => QueryValues(parameter, queryValues)
                .Select(value => $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(value)}"))
            .ToList();
        if (query.Count > 0)
        {
            resolved += (resolved.Contains('?') ? "&" : "?") + string.Join("&", query);
        }

        return new Uri(resolved);
    }

    private static IReadOnlyList<string> QueryValues(
        ParameterDeclaration parameter,
        IReadOnlyDictionary<string, IReadOnlyList<string>> queryValues)
    {
        if (queryValues.TryGetValue(parameter.Name, out var values))
        {
            return values;
        }

        return parameter.DefaultValue is null ? [] : [JsonSerializer.Deserialize<string>(parameter.DefaultValue)];
    }

    public static IReadOnlyList<string> PlaceholderNames(string url)
    {
        return PlaceholderPattern().Matches(url).Select(match => match.Value[1..^1]).Distinct().ToList();
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex PlaceholderPattern();
}
