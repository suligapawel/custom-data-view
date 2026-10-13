using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Http;

public sealed class ExternalApiClient(IHttpClientFactory httpClientFactory) : IExternalApiClient
{
    public const string HttpClientName = "CustomDataView.ExternalApi";

    public async Task<ExternalApiResult> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ExternalApiResult((int)response.StatusCode, JsonNode.Parse(content));
    }
}
