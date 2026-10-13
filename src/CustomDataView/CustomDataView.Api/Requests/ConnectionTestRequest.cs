using System.Text.Json;
using CustomDataView.Core.Connections.Calls;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionTestRequest(ConnectionParametersRequest Parameters)
{
    public static ParameterValuesInput ValuesOf(ConnectionTestRequest request)
    {
        var parameters = request?.Parameters;
        return new ParameterValuesInput(
            parameters?.Path ?? new Dictionary<string, JsonElement>(),
            parameters?.Query ?? new Dictionary<string, JsonElement>(),
            parameters?.Body ?? new Dictionary<string, JsonElement>());
    }
}

public sealed record ConnectionParametersRequest(
    Dictionary<string, JsonElement> Path,
    Dictionary<string, JsonElement> Query,
    Dictionary<string, JsonElement> Body);
