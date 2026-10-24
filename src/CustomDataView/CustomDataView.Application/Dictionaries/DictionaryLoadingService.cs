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
    }
}
