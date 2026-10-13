using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Calls;

public sealed class GetConnection(GetConnectionDefinition definition, IExternalApiClient client) : IConnection
{
    public Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, UrlTemplate.Resolve(definition.Url, parameters.Path, definition.QueryParameters, parameters.Query));
        return client.Send(request, cancellationToken);
    }
}
