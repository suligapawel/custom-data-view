using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Connections.Calls;
using Microsoft.Extensions.Options;

namespace CustomDataView.Infrastructure.ExternalApi;

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
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return ExternalApiResult.Failure(ExternalApiErrorCodes.UnexpectedStatus, (int)response.StatusCode);
            }

            var content = await ReadLimited(response.Content, timeout.Token);
            stopwatch.Stop();
            if (content is null)
            {
                return ExternalApiResult.Failure(ExternalApiErrorCodes.ResponseTooLarge);
            }

            return ExternalApiResult.Success((int)response.StatusCode, stopwatch.ElapsedMilliseconds, JsonNode.Parse(content));
        }
        catch (JsonException)
        {
            return ExternalApiResult.Failure(ExternalApiErrorCodes.InvalidJson);
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

    private async Task<byte[]> ReadLimited(HttpContent content, CancellationToken cancellationToken)
    {
        var maxBytes = options.Value.MaxResponseBytes;
        if (content.Headers.ContentLength > maxBytes)
        {
            return null;
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
