using System.Text.Json;
using CustomDataView.Application.Connections.Calls;

namespace CustomDataView.WebApi.Requests;

public sealed record ConnectionTestRequest(ConnectionParametersRequest Parameters)
{
    public static ParameterValuesInput ValuesOf(ConnectionTestRequest request) => ValuesOf(request?.Parameters);

    public static ParameterValuesInput ValuesOf(ConnectionParametersRequest parameters)
        => new ParameterValuesInput(
            parameters?.Path ?? new Dictionary<string, JsonElement>(),
            parameters?.Query ?? new Dictionary<string, JsonElement>(),
            parameters?.Body ?? new Dictionary<string, JsonElement>());
}

public sealed record ConnectionParametersRequest(
    Dictionary<string, JsonElement> Path,
    Dictionary<string, JsonElement> Query,
    Dictionary<string, JsonElement> Body);
