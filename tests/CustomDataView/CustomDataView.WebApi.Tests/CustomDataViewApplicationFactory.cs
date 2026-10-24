using CustomDataView.Application.Dictionaries;
using CustomDataView.Domain.Repositories;
using CustomDataView.Infrastructure.ExternalApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CustomDataView.WebApi.Tests;

public class CustomDataViewApplicationFactory : WebApplicationFactory<Program>
{
    private ExternalApiOptions defaultExternalApiOptions;

    public FakeExternalApi ExternalApi { get; } = new();

    public TestTimeProvider Time { get; } = new();

    public TestDictionaryLoadQueue DictionaryLoads => Services.GetRequiredService<TestDictionaryLoadQueue>();

    public void Reset()
    {
        Services.GetRequiredService<TestConnectionRepository>().Clear();
        Services.GetRequiredService<TestTileRepository>().Clear();
        Services.GetRequiredService<MapperDictionaries>().Clear();
        DictionaryLoads.Clear();
        ExternalApi.Reset();
        Time.Reset();
        var options = Services.GetRequiredService<IOptions<ExternalApiOptions>>().Value;
        defaultExternalApiOptions ??= new ExternalApiOptions
        {
            Timeout = options.Timeout,
            MaxResponseBytes = options.MaxResponseBytes
        };
        options.Timeout = defaultExternalApiOptions.Timeout;
        options.MaxResponseBytes = defaultExternalApiOptions.MaxResponseBytes;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);
            services.AddSingleton<TestConnectionRepository>();
            services.AddSingleton<IConnectionRepository>(provider => provider.GetRequiredService<TestConnectionRepository>());
            services.AddSingleton<TestTileRepository>();
            services.AddSingleton<ITileRepository>(provider => provider.GetRequiredService<TestTileRepository>());
            services.AddSingleton<TestDictionaryLoadQueue>();
            services.AddSingleton<IDictionaryLoadQueue>(provider => provider.GetRequiredService<TestDictionaryLoadQueue>());
            services.AddHttpClient(ExternalApiClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => ExternalApi)
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        });
    }
}
