using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Connections.Calls;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionTestRequest(ConnectionParametersRequest Parameters)
{
    public static ConnectionParameterValues ValuesOf(ConnectionTestRequest request)
    {
        var parameters = request?.Parameters;
        return new ConnectionParameterValues(
            parameters?.Path ?? new Dictionary<string, string>(),
            (parameters?.Query ?? new Dictionary<string, JsonElement>())
                .ToDictionary(pair => pair.Key, pair => QueryValues(pair.Value)),
            (parameters?.Body ?? new Dictionary<string, JsonElement>())
                .ToDictionary(pair => pair.Key, pair => JsonNode.Parse(pair.Value.GetRawText())));
    }

    private static IReadOnlyList<string> QueryValues(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString()).ToList()
            : [value.GetString()];
    }
}

public sealed record ConnectionParametersRequest(
    Dictionary<string, string> Path,
    Dictionary<string, JsonElement> Query,
    Dictionary<string, JsonElement> Body);
