using CustomDataView.Core.Connections.Http;
using CustomDataView.Core.Connections.Persistence;
using CustomDataView.Core.Tiles.Persistence;
using CustomDataView.Core.Views.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CustomDataView.Api.Tests;

public class CustomDataViewApplicationFactory : WebApplicationFactory<Program>
{
    public FakeExternalApi ExternalApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IConnectionRepository, TestConnectionRepository>();
            services.AddSingleton<ITileRepository, TestTileRepository>();
            services.AddSingleton<TestViewRepository>();
            services.AddSingleton<IViewRepository>(provider => provider.GetRequiredService<TestViewRepository>());
            services.AddHttpClient(ExternalApiClient.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => ExternalApi);
        });
    }
}
