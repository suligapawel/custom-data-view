using System.Text.Json;
using System.Text.RegularExpressions;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services;

public static partial class UrlTemplate
{
    public static bool HasPlaceholdersOutsidePath(string url)
    {
        var queryStart = url.IndexOfAny(['?', '#']);
        return queryStart >= 0 && url.IndexOfAny(['{', '}'], queryStart) >= 0;
    }

    public static bool HasUnclosedBraces(string url)
        => PlaceholderPattern().Replace(url, string.Empty).IndexOfAny(['{', '}']) >= 0;

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
            .SelectMany(parameter => QueryValues(parameter, queryValues)
                .Select(value => $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(value)}"))
            .ToList();
        if (query.Count > 0)
        {
            resolved += (resolved.Contains('?') ? "&" : "?") + string.Join("&", query);
        }

        return new Uri(resolved);
    }

    public static IReadOnlyList<string> PlaceholderNames(string url)
        => PlaceholderPattern().Matches(url).Select(match => match.Value[1..^1]).Distinct().ToList();

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
        ParameterDeclaration parameter,
        IReadOnlyDictionary<string, IReadOnlyList<string>> queryValues)
    {
        if (queryValues.TryGetValue(parameter.Name, out var values))
        {
            return values;
        }

        return parameter.DefaultValue is null ? [] : [JsonSerializer.Deserialize<string>(parameter.DefaultValue)];
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex PlaceholderPattern();
}
