using System.Text.Json.Nodes;

namespace CustomDataView.Application.Connections.Calls;

public sealed record ConnectionParameterValues(
    IReadOnlyDictionary<string, string> Path,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Query,
    IReadOnlyDictionary<string, JsonNode> Body);
