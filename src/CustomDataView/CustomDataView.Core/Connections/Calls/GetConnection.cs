using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Http;

namespace CustomDataView.Core.Connections.Calls;

public sealed class GetConnection(GetConnectionDefinition definition, IExternalApiClient client) : IConnection
{
    public Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken)
    {
        var request = ConnectionRequestBuilder.Build(HttpMethod.Get, definition, parameters, null);
        
        return client.Send(request, cancellationToken);
    }
}
