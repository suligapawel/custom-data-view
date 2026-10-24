using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomDataView.Application.Dictionaries;

public sealed class DictionaryLoadingService(
    DictionaryLoader loader,
    DictionaryLoadQueue queue,
    ILogger<DictionaryLoadingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await loader.LoadAll(stoppingToken);
        await foreach (var references in queue.ReadAll(stoppingToken))
        {
            try
            {
                await loader.Load(references, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    "Dictionary loading failed for tiles {TileIds} with {ExceptionType}.",
                    references.Select(reference => reference.TileId).Distinct(),
                    exception.GetType().Name);
            }
        }
    }
}
