using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Calls;

public sealed record ParameterValuesInput(
    IReadOnlyDictionary<string, JsonElement> Path,
    IReadOnlyDictionary<string, JsonElement> Query,
    IReadOnlyDictionary<string, JsonElement> Body)
{
    public ConnectionParameterValues ToValues()
        => new ConnectionParameterValues(
            Path.ToDictionary(pair => pair.Key, pair => pair.Value.GetString()),
            Query.ToDictionary(pair => pair.Key, pair => QueryValues(pair.Value)),
            Body.ToDictionary(pair => pair.Key, pair => JsonNode.Parse(pair.Value.GetRawText())));

    private static IReadOnlyList<string> QueryValues(JsonElement value)
        => value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString()).ToList()
            : [value.GetString()];
}
