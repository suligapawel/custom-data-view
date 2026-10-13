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
        var url = UrlTemplate.Resolve(definition.Url, parameters.Path, definition.QueryParameters, parameters.Query);
        var request = new HttpRequestMessage(method, url) { Content = content };
        foreach (var header in definition.Headers)
        {
            if (header.Name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                content?.Headers.Remove(header.Name);
                content?.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        return request;
    }
}
