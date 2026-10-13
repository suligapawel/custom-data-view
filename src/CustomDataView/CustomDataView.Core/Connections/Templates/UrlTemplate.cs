using System.Text.RegularExpressions;

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

    public static IReadOnlyList<string> PlaceholderNames(string url)
    {
        return PlaceholderPattern().Matches(url).Select(match => match.Value[1..^1]).Distinct().ToList();
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex PlaceholderPattern();
}
