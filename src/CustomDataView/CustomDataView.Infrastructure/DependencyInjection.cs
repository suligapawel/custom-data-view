using CustomDataView.Application.Connections.Calls;
using CustomDataView.Domain.Repositories;
using CustomDataView.Infrastructure.ExternalApi;
using CustomDataView.Infrastructure.Persistence;
using CustomDataView.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CustomDataView.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CustomDataView";

    public static IServiceCollection AddCustomDataViewInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(
            provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string \"{ConnectionStringName}\" is not configured.")));
        services.AddDbContextFactory<CustomDataViewDbContext>(
            (provider, options) => options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>(), CustomDataViewDbContext.ConfigureNpgsql));
        services.AddSingleton<IConnectionRepository, PostgresConnectionRepository>();
        services.AddSingleton<ITileRepository, PostgresTileRepository>();
        services.AddSingleton<IViewRepository, PostgresViewRepository>();
        services.AddSingleton<IConnectionFactory, ConnectionFactory>();
        services.AddSingleton<IExternalApiClient, ExternalApiClient>();
        services.AddOptions<ExternalApiOptions>().BindConfiguration(ExternalApiOptions.SectionName);
        services.AddHttpClient(ExternalApiClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        return services;
    }
}
