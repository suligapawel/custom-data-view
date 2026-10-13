using System.Text.RegularExpressions;

namespace CustomDataView.Core.Connections.Validation;

public static partial class ParameterNames
{
    public static bool IsSnakeCase(string name) => name is not null && SnakeCasePattern().IsMatch(name);

    [GeneratedRegex("^[a-z][a-z0-9_]{0,49}$")]
    private static partial Regex SnakeCasePattern();
}
