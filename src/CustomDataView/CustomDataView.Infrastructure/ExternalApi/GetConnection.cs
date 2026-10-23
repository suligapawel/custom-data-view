using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Infrastructure.ExternalApi;

public sealed class GetConnection(GetConnectionDefinition definition, IExternalApiClient client) : IConnection
{
    public Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken)
    {
        var request = ConnectionRequestBuilder.Build(HttpMethod.Get, definition, parameters, null);
        
        return client.Send(request, cancellationToken);
    }
}
