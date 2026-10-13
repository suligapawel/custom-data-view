using System.Diagnostics;
using System.Text.Json.Nodes;

namespace CustomDataView.Core.Connections.Http;

public sealed class ExternalApiClient(IHttpClientFactory httpClientFactory) : IExternalApiClient
{
    public const string HttpClientName = "CustomDataView.ExternalApi";

    public async Task<ExternalApiResult> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(HttpClientName);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();
            return ExternalApiResult.Success((int)response.StatusCode, stopwatch.ElapsedMilliseconds, JsonNode.Parse(content));
        }
        catch (HttpRequestException)
        {
            return ExternalApiResult.Failure(ExternalApiErrorCodes.SourceUnavailable);
        }
    }
}
