using System.Text;
using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Calls;

public sealed class PostConnection(PostConnectionDefinition definition, IExternalApiClient client) : IConnection
{
    public Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken)
    {
        var url = UrlTemplate.Resolve(definition.Url, parameters.Path, definition.QueryParameters, parameters.Query);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (definition.Body is not null)
        {
            var body = BodyTemplate.Resolve(definition.Body, definition.BodyParameters, parameters.Body);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return client.Send(request, cancellationToken);
    }
}
