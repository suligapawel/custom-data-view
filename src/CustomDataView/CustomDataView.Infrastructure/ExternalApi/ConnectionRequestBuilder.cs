using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Infrastructure.ExternalApi;

public static class ConnectionRequestBuilder
{
    public static HttpRequestMessage Build(
        HttpMethod method,
        ConnectionDefinition definition,
        ConnectionParameterValues parameters,
        HttpContent content)
    {
        var url = UrlTemplate.Resolve(
            definition.Url,
            definition.PathParameters,
            parameters.Path,
            definition.QueryParameters,
            parameters.Query);
        
        return new HttpRequestMessage(method, url) { Content = content };
    }
}
