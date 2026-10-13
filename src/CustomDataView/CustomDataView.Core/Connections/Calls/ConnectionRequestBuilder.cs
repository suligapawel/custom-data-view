using CustomDataView.Core.Connections.Definitions;
using CustomDataView.Core.Connections.Templates;

namespace CustomDataView.Core.Connections.Calls;

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
