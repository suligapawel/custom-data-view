using System.Collections.Concurrent;
using System.Text.Json;
using CustomDataView.Application.Connections.Calls;

namespace CustomDataView.Application.Tiles.Preview;

public sealed class PreviewContext(TilePreviewValues values, TimeProvider time, CancellationToken cancellationToken)
{
    public const int MaxExternalCalls = 200;
    public const int MaxParallelCalls = 8;
    public static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, Lazy<Task<ExternalApiResult>>> calls = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim throttle = new(MaxParallelCalls);
    private readonly long started = time.GetTimestamp();
    private int budget;

    public TilePreviewValues Values { get; } = values;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public bool TryStartEmbedding(int calls)
    {
        if (time.GetElapsedTime(started) >= MaxDuration)
        {
            return false;
        }

        while (true)
        {
            var current = Volatile.Read(ref budget);
            if (calls > 0 && current + calls > MaxExternalCalls)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref budget, current + calls, current) == current)
            {
                return true;
            }
        }
    }

    public Task<ExternalApiResult> Call(
        Guid connectionId,
        ConnectionParameterValues parameters,
        bool embedded,
        Func<Task<ExternalApiResult>> call)
        => calls.GetOrAdd(Key(connectionId, parameters), _ => new Lazy<Task<ExternalApiResult>>(() =>
        {
            if (!embedded)
            {
                Interlocked.Increment(ref budget);
                return call();
            }

            return Throttled(call);
        })).Value;

    private async Task<ExternalApiResult> Throttled(Func<Task<ExternalApiResult>> call)
    {
        await throttle.WaitAsync(CancellationToken);
        try
        {
            return await call();
        }
        finally
        {
            throttle.Release();
        }
    }

    private static string Key(Guid connectionId, ConnectionParameterValues parameters)
        => JsonSerializer.Serialize(new
        {
            connectionId,
            path = parameters.Path.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            query = parameters.Query.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            body = parameters.Body.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => (pair.Key, pair.Value?.ToJsonString()))
        });
}
