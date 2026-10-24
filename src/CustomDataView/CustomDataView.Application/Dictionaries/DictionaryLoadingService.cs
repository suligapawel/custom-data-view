using CustomDataView.Domain.ValueObjects;
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
        await LoadSafely(() => loader.LoadAll(stoppingToken), []);
        await foreach (var references in queue.ReadAll(stoppingToken))
        {
            await LoadSafely(() => loader.Load(references, stoppingToken), references);
        }
    }

    private async Task LoadSafely(Func<Task> load, IReadOnlyList<DictionaryReference> references)
    {
        try
        {
            await load();
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
