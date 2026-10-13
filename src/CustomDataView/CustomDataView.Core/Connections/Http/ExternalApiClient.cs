using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace CustomDataView.Core.Connections.Http;

public sealed class ExternalApiClient(IHttpClientFactory httpClientFactory, IOptions<ExternalApiOptions> options)
    : IExternalApiClient
{
    public const string HttpClientName = "CustomDataView.ExternalApi";

    public async Task<ExternalApiResult> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(HttpClientName);
        client.Timeout = Timeout.InfiniteTimeSpan;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.Timeout);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, timeout.Token);
            var content = await response.Content.ReadAsStringAsync(timeout.Token);
            stopwatch.Stop();
            return ExternalApiResult.Success((int)response.StatusCode, stopwatch.ElapsedMilliseconds, JsonNode.Parse(content));
        }
        catch (HttpRequestException)
        {
            return ExternalApiResult.Failure(ExternalApiErrorCodes.SourceUnavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ExternalApiResult.Failure(ExternalApiErrorCodes.SourceUnavailable);
        }
    }
}
