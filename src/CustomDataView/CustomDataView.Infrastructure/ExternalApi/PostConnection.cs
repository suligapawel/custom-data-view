using System.Text;
using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Infrastructure.ExternalApi;

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
