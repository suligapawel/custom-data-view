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
        IReadOnlyList<ParameterDeclaration> pathParameters,
        IReadOnlyDictionary<string, string> pathValues,
        IReadOnlyList<ParameterDeclaration> queryParameters,
        IReadOnlyDictionary<string, IReadOnlyList<string>> queryValues)
    {
        var resolved = PlaceholderPattern().Replace(
            url,
            match => Uri.EscapeDataString(PathValue(match.Value[1..^1], pathParameters, pathValues)));
        var query = queryParameters
            .SelectMany((parameter, index) => QueryValues(queryParameters, index, queryValues)
                .Select(value => $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(value)}"))
            .ToList();
        if (query.Count > 0)
        {
            resolved += (resolved.Contains('?') ? "&" : "?") + string.Join("&", query);
        }

        return new Uri(resolved);
    }

    public static IReadOnlyList<string> PlaceholderNames(string url)
    {
        return PlaceholderPattern().Matches(url).Select(match => match.Value[1..^1]).Distinct().ToList();
    }

    private static string PathValue(
        string name,
        IReadOnlyList<ParameterDeclaration> pathParameters,
        IReadOnlyDictionary<string, string> pathValues)
    {
        if (pathValues.TryGetValue(name, out var value))
        {
            return value;
        }

        var defaultValue = pathParameters.Single(parameter => parameter.Name == name).DefaultValue
            ?? throw new InvalidOperationException($"Path parameter {name} has no value.");
        return JsonSerializer.Deserialize<string>(defaultValue);
    }

    private static IReadOnlyList<string> QueryValues(
        IReadOnlyList<ParameterDeclaration> queryParameters,
        int index,
        IReadOnlyDictionary<string, IReadOnlyList<string>> queryValues)
    {
        var parameter = queryParameters[index];
        if (queryValues.TryGetValue(parameter.Name, out var values))
        {
            var isFirstDeclaration = queryParameters.Take(index).All(previous => previous.Name != parameter.Name);
            return isFirstDeclaration ? values : [];
        }

        return parameter.DefaultValue is null ? [] : [JsonSerializer.Deserialize<string>(parameter.DefaultValue)];
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex PlaceholderPattern();
}
