namespace CustomDataView.Core.Connections.Calls;

public sealed record ConnectionParameterValues(
    IReadOnlyDictionary<string, string> Path,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Query);
