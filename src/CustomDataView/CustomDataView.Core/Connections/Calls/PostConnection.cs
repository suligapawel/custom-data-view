using System.Text;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Calls;

public sealed class PostConnection(PostConnectionDefinition definition, IExternalApiClient client) : IConnection
{
    private const string JsonMediaType = "application/json";

    public Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken)
    {
        var content = definition.Body is null
            ? null
            : new StringContent(
                BodyTemplate.Resolve(definition.Body, definition.BodyParameters, parameters.Body),
                Encoding.UTF8,
                JsonMediaType);
        
        var request = ConnectionRequestBuilder.Build(HttpMethod.Post, definition, parameters, content);
        
        return client.Send(request, cancellationToken);
    }
}
