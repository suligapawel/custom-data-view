using System.Text.RegularExpressions;

namespace CustomDataView.Core.Connections.Validation;

public static partial class ParameterNames
{
    public static bool IsSnakeCase(string name)
    {
        return name is not null && SnakeCasePattern().IsMatch(name);
    }

    public static bool IsHeaderToken(string name)
    {
        return name is not null && HeaderTokenPattern().IsMatch(name);
    }

    [GeneratedRegex("^[!#$%&'*+\\-.^_`|~0-9A-Za-z]+$")]
    private static partial Regex HeaderTokenPattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,49}$")]
    private static partial Regex SnakeCasePattern();
}
