using CustomDataView.Core.Connections.Calls;

namespace CustomDataView.Api.Requests;

public sealed record ConnectionTestRequest(ConnectionParametersRequest Parameters)
{
    public static ConnectionParameterValues ValuesOf(ConnectionTestRequest request)
    {
        return new ConnectionParameterValues(request?.Parameters?.Path ?? new Dictionary<string, string>());
    }
}

public sealed record ConnectionParametersRequest(Dictionary<string, string> Path);
